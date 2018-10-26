using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateRelationship
{
    class SQLHelper
    {
        private SqlConnection con = null;
        private SqlCommand com = null;
        public SQLHelper(string constr)
        {
            this.con = new SqlConnection(constr);
            this.con.Open();
            this.com = new SqlCommand();
            this.com.Connection = this.con;
        }

        public void CheckDataTable(TableType tableType)
        {
            string tableName = "";
            switch (tableType)
            {
                case TableType.JobTable: tableName = Constants.JobTableTitle; break;
                case TableType.OriginalTable: tableName = Constants.OriginalTableTitle; break;
                case TableType.ReportTable: tableName = Constants.ReportTableTitle; break;
                    //case TableType.UpdateTable: tableName = Constants.UpdateTableTitle; break;
            }
            Program.logger.Debug("Start check database {0}", tableName);
            Console.WriteLine("Start check database {0}", tableName);
            string sqlStr = string.Format("select * from sys.tables where name='{0}'", tableName);
            this.com.CommandText = sqlStr;
            bool exist = true;
            using (SqlDataReader reader = this.com.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    exist = false;
                }
            }
            if (!exist)
            {
                switch (tableType)
                {
                    case TableType.JobTable:
                        this.com.CommandText = Constants.CreateJobTable;
                        this.com.ExecuteNonQuery();
                        break;
                    case TableType.OriginalTable:
                        this.com.CommandText = Constants.CreateOriginalTable;
                        this.com.ExecuteNonQuery();
                        break;
                    case TableType.ReportTable:
                        this.com.CommandText = Constants.CreateReportTable;
                        this.com.ExecuteNonQuery();
                        break;
                        //case TableType.UpdateTable:
                        //    this.com.CommandText = Constants.CreateUpdateTable;
                        //    this.com.ExecuteNonQuery();
                        //    break;
                }
            }
            Console.WriteLine("Finish check database {0}", tableName);
            Program.logger.Debug("Check database {0} over.", tableName);
        }

        internal List<ResultInfo> SearchItems(string jobId, SPUtility sputility, ConfigInfo configInfo)
        {
            List<ResultInfo> results = new List<ResultInfo>();
            string sqlStr = string.Format("select * from {0} where JobId='{1}'", Constants.OriginalTableTitle, jobId);
            this.com.CommandText = sqlStr;
            using (SqlDataReader result = this.com.ExecuteReader())
            {
                while (result.Read())
                {
                    string itemId = result["ItemId"].ToString();
                    try
                    {
                        string updateValue = result["RelatedValue"].ToString();
                        Program.logger.Debug("Return the item info. Item id:{0}, new value: {1}.", itemId, updateValue);
                        ResultInfo resultInfo = sputility.UpdateItemWithInfo(itemId, updateValue, configInfo);
                        results.Add(resultInfo);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(string.Format("Error: {0}, ItemId: {1}", e.Message, itemId));
                        Program.logger.Warn("Update item fialed. Item id: {0}, Exception :{1}", itemId, e.Message);
                    }
                }
            }
            return results;
        }

        public void InsertItemInfo(ScanItemResult itemInfo, string jobId, string webUrl, string listTitle, string linkValue)
        {
            try
            {
                string str = string.Format("insert into {0} ([JobId],[HPTrimID],[ItemUrl],[SiteUrl],[ListTitle],[OriginalRelatedValue],[RelatedValue],[ItemId]) values ('{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", Constants.OriginalTableTitle, jobId, itemInfo.HPTrimId,
                    itemInfo.ItemUrl, webUrl, listTitle, itemInfo.OriginalValue, linkValue, itemInfo.ItemId);
                this.com.CommandText = str;
                this.com.ExecuteNonQuery();
                Program.logger.Info("Insert item info to database. Item id: {0}, Old value: {1}, New value: {2}.", itemInfo.ItemId, itemInfo.OriginalValue, linkValue);
            }
            catch (Exception e)
            {
                Program.logger.Warn("Insert item info to database failed. Item id: {0}, Old value: {1}, New value: {2}, Exception: {3}.", itemInfo.ItemId, itemInfo.OriginalValue, linkValue, e.Message);
            }
        }
        public void InsertReportInfo(ResultInfo resultInfo, string jobId, string scanJobId)
        {
            Program.logger.Debug("Insert report to report database. item id: {0}", resultInfo.ItemId);
            try
            {
                string str = string.Format("insert into {0} ([ScanJobId],[JobId],[ListUrl],[ItemId],[Result],[Message]) values ('{1}','{2}','{3}','{4}','{5}','{6}')", Constants.ReportTableTitle, scanJobId, jobId, resultInfo.ListUrl, resultInfo.ItemId, resultInfo.Result, resultInfo.Message);
                this.com.CommandText = str;
                this.com.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Program.logger.Warn("Insert report to report database failed. item id: {0}, Exception: {1}", resultInfo.ItemId, e.Message);
            }
        }

        public void ExecuteNonQuery(string str)
        {
            this.com.CommandText = str;
            this.com.ExecuteNonQuery();
        }
        public string SearchJobId(string searchStr)
        {
            this.com.CommandText = searchStr;
            try
            {
                using (SqlDataReader result = this.com.ExecuteReader())
                {
                    result.Read();
                    return result["JobId"].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Program.logger.Error(e.Message);
                return null;
            }
        }

        internal List<HPResultInfo> RetrieveSourceDBWithInfo(ScanItemResult itemResult, string sourceDBName)
        {
            List<HPResultInfo> results = new List<HPResultInfo>();
            string sqlStr = string.Format("select destId,typeof from {0} where sourceId = '{1}'", sourceDBName, itemResult.HPTrimId);
            this.com.CommandText = sqlStr;
            try
            {
                using (SqlDataReader result = this.com.ExecuteReader())
                {
                    while (result.Read())
                    {
                        HPResultInfo info = new HPResultInfo();
                        info.HPId = result["destId"].ToString();
                        info.RelateType = result["typeof"].ToString();
                        results.Add(info);
                    }
                }
                return results;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Program.logger.Error(e.Message);
                return null;
            }
        }

        public void Close()
        {
            this.com.Dispose();
            if (this.con.State == ConnectionState.Open)
            {
                this.con.Close();
            }
        }
    }
    enum TableType
    {
        JobTable = 1,
        OriginalTable = 2,
        ReportTable = 3,
    }
    class HPResultInfo
    {
        public string Title;
        public string HPId;
        public string RelateType;
        public string Link;
    }
}
