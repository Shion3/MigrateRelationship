using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MigrateRelationship
{
    class Program
    {
        public static LoggerHandler logger = new LoggerHandler(MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            SQLHelper sqlHelper = null;
            try
            {
                Program.logger.Debug("Start read config.xml");
                ConfigReader configHelper = new ConfigReader("Config.xml");
                ConfigInfo configInfo = configHelper.GetConfigInfo();
                Program.logger.Debug("End read config.xml");
                if (configInfo.DBWindowsMode)
                {
                    sqlHelper = new SQLHelper(string.Format("server={0};database={1};integrated security=SSPI", configInfo.DBServer, configInfo.DBDatabaseName));
                }
                else
                {
                    sqlHelper = new SQLHelper(string.Format("server={0};database={1};uid={2};pwd={3}", configInfo.DBServer, configInfo.DBDatabaseName, configInfo.DBUserName, configInfo.DBPassword));
                }
                //true scan item,false update item
                if (configInfo.JobType)
                {
                    Program.logger.Debug("Start scan job.");
                    //生成并添加jobId到数据库中JobTable
                    sqlHelper.CheckDataTable(TableType.JobTable);
                    sqlHelper.CheckDataTable(TableType.OriginalTable);
                    string jobId = InitiateJobId(sqlHelper, JobType.ScanItemJob);
                    Console.WriteLine("Start load Sharepoint info...");
                    ScanItems(configInfo, sqlHelper, jobId);
                    UpdateJobId(jobId, sqlHelper);
                    Program.logger.Debug("End scan job.");
                }
                else
                {
                    Program.logger.Debug("Start update job.");
                    sqlHelper.CheckDataTable(TableType.JobTable);
                    sqlHelper.CheckDataTable(TableType.ReportTable);
                    string jobId = InitiateJobId(sqlHelper, JobType.UpdateItemJob);
                    Console.WriteLine("Start load Sharepoint info...");
                    UpdateItems(configInfo, sqlHelper, jobId);
                    UpdateJobId(jobId, sqlHelper);
                    Program.logger.Debug("End update job.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: {0}", e.Message);
                Program.logger.Error("Warning: {0}", e.Message);
            }
            finally
            {
                sqlHelper.Close();
                Console.WriteLine("");
                Console.WriteLine("Job Complete. Please enter any key to exist.");
                Program.logger.Debug("Job finished.");
                Program.logger.Dispose();
                Console.ReadKey();
            }
        }

        private static void UpdateItems(ConfigInfo configInfo, SQLHelper sqlHelper, string currentJobId)
        {
            SPUtility sputility = null;
            try
            {
                string jobId = RetrieveJobId(sqlHelper);
                sputility = new SPUtility(configInfo);
                List<ResultInfo> results = sqlHelper.SearchItems(jobId, sputility, configInfo);
                foreach (ResultInfo result in results)
                {
                    sqlHelper.InsertReportInfo(result, currentJobId, jobId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Update Itmes Warning : {0}", e.Message);
                Program.logger.Error("Update items Warning : {0}", e.Message);
            }
            finally
            {
                if (sputility != null)
                {
                    sputility.Close();
                }
            }
        }

        private static string RetrieveJobId(SQLHelper sqlHelper)
        {
            Program.logger.Debug("Retrieve the lastest update job id.");
            string sqlStr = string.Format("select JobId from {0} where JobType='{1}' and EndTime is not null order by StartTime desc", Constants.JobTableTitle, JobType.ScanItemJob);
            string jobId = sqlHelper.SearchJobId(sqlStr);
            Program.logger.Debug("Finish retrieve the lastest update job id. {0}", jobId);
            return jobId;
        }

        private static void ScanItems(ConfigInfo configInfo, SQLHelper sqlHelper, string jobId)
        {
            SecureString secureString = new SecureString();
            string password = configInfo.SPPassWord;
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                secureString.AppendChar(c);
            }
            using (ClientContext context = new ClientContext(configInfo.SPSiteUrl))
            {
                context.Credentials = new SharePointOnlineCredentials(configInfo.SPUserName, secureString);
                List list = context.Web.Lists.GetByTitle(configInfo.SPListTitle);
                context.Load(list);
                context.ExecuteQuery();
                Console.WriteLine(list.Title);

                List<ListItem> items = RetrieveItems(context, list);

                foreach (ListItem item in items)
                {
                    try
                    {
                        ScanItemResult itemResult = SPUtility.AssembleSPItemInfo(configInfo, list, item);
                        List<HPResultInfo> result = sqlHelper.RetrieveSourceDBWithInfo(itemResult, Constants.SourceTableTitle);
                        string relateValue = SPUtility.RetrieveItemsCombinNewVlaue(configInfo, result, list);
                        sqlHelper.InsertItemInfo(itemResult, jobId, context.Url, list.Title, relateValue);
                        //记录原始数据,计算更新数据添加到OriginalTable中
                        if (item.FileSystemObjectType == FileSystemObjectType.File)
                        {
                            Console.WriteLine(item.Id);
                        }
                        else if (item.FileSystemObjectType == FileSystemObjectType.Folder)
                        {
                            RetrieveFolder(context, list, item, sqlHelper, jobId, configInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(string.Format("Warning: {0}, ItemId: {1}", e.Message, item.Id));
                        Program.logger.Warn("Warning: {0}, ItemId: {1}", e.Message, item.Id);
                    }
                }
            }
        }

        private static List<ListItem> RetrieveItems(ClientContext context, List list, string folderUrl = null)
        {
            Program.logger.Debug("Retrieve sharepoint items from {0}", folderUrl == null ? "Root Folder" : folderUrl);
            List<ListItem> itemCollection = new List<ListItem>();
            CamlQuery query = new CamlQuery() { };
            query.ViewXml = "<View><RowLimit>500</RowLimit></View>";
            if (folderUrl != null)
            {
                query.FolderServerRelativeUrl = folderUrl;
            }
            ListItemCollection items = list.GetItems(query);
            context.Load(items);
            context.ExecuteQuery();
            query.ListItemCollectionPosition = items.ListItemCollectionPosition;
            foreach (ListItem item in items)
            {
                itemCollection.Add(item);
            }
            while (query.ListItemCollectionPosition != null)
            {
                items = list.GetItems(query);
                context.Load(items);
                context.ExecuteQuery();
                query.ListItemCollectionPosition = items.ListItemCollectionPosition;
                foreach (ListItem item in items)
                {
                    itemCollection.Add(item);
                }
            }
            return itemCollection;
        }

        private static string InitiateJobId(SQLHelper sqlHelper, JobType jobType)
        {
            DateTime now = DateTime.Now;
            string jobId = now.ToString("yyyyMMddHHmmss");
            Program.logger.Debug("Insert job id to job table. JobId: {0}", jobId);
            string sqlStr = string.Format("insert into {0} (JobId,StartTime,JobType) values ('{1}','{2}','{3}')", Constants.JobTableTitle, jobId, now, jobType);
            sqlHelper.ExecuteNonQuery(sqlStr);
            return jobId;
        }

        private static void UpdateJobId(string jobId, SQLHelper sqlHelper)
        {
            string sqlStr = string.Format("update {0} set EndTime='{1}' where JobId='{2}'", Constants.JobTableTitle, DateTime.Now, jobId, JobType.ScanItemJob);
            sqlHelper.ExecuteNonQuery(sqlStr);
        }

        private static void RetrieveFolder(ClientContext context, List list, ListItem item, SQLHelper sqlHelper, string jobId, ConfigInfo config)
        {
            Folder folder = item.Folder;
            context.Load(folder);
            context.ExecuteQuery();
            //CamlQuery query = new CamlQuery() { };
            //query.FolderServerRelativeUrl = folder.ServerRelativeUrl;
            //ListItemCollection items = list.GetItems(query);
            //context.Load(items);
            //context.ExecuteQuery();
            List<ListItem> items = RetrieveItems(context, list, folder.ServerRelativeUrl);

            foreach (ListItem current in items)
            {
                try
                {
                    ScanItemResult itemResult = SPUtility.AssembleSPItemInfo(config, list, current);
                    List<HPResultInfo> result = sqlHelper.RetrieveSourceDBWithInfo(itemResult, Constants.SourceTableTitle);
                    string relateValue = SPUtility.RetrieveItemsCombinNewVlaue(config, result, list);
                    sqlHelper.InsertItemInfo(itemResult, jobId, context.Url, list.Title, relateValue);
                    if (current.FileSystemObjectType == FileSystemObjectType.File)
                    {
                        Console.WriteLine(item.Id);
                    }
                    else if (current.FileSystemObjectType == FileSystemObjectType.Folder)
                    {
                        RetrieveFolder(context, list, current, sqlHelper, jobId, config);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Warning: {0}, ItemId: {1}", e.Message, current.Id));
                    Program.logger.Warn("Warning: {0}, ItemId: {1}", e.Message, current.Id);
                }
            }
        }
    }

    class ResultInfo
    {
        public string ListUrl;
        public string ItemId;
        public string Result;
        public string Message;
        public ResultInfo(string listUrl, string itemId, string result, string message)
        {
            this.ListUrl = listUrl;
            this.ItemId = itemId;
            this.Result = result;
            this.Message = message;
        }
    }
}
