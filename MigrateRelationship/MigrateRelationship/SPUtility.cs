using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MigrateRelationship
{
    class SPUtility
    {
        ClientContext context;
        List list;
        public SPUtility(ConfigInfo configInfo)
        {
            SecureString secureString = new SecureString();
            string password = configInfo.SPPassWord;
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                secureString.AppendChar(c);
            }
            this.context = new ClientContext(configInfo.SPSiteUrl);
            context.Credentials = new SharePointOnlineCredentials(configInfo.SPUserName, secureString);
            this.list = context.Web.Lists.GetByTitle(configInfo.SPListTitle);
            context.Load(this.list);
            context.ExecuteQuery();
            Console.WriteLine(this.list.Title);
        }
        public void Close()
        {
            this.context.Dispose();
        }
        public static ScanItemResult AssembleSPItemInfo(ConfigInfo configInfo, List list, ListItem item)
        {
            ScanItemResult result = new ScanItemResult();
            result.ItemUrl = configInfo.SPSiteUrl.TrimEnd('/') + "/" + list.Title + @"/Forms/DispForm.aspx?ID=" + item.Id;
            result.HPTrimId = item[configInfo.PrimaryKey] == null ? "" : item[configInfo.PrimaryKey].ToString();
            result.OriginalValue = item[configInfo.ColumnName] == null ? "" : item[configInfo.ColumnName].ToString();
            result.ItemId = item.Id.ToString();
            return result;
        }

        public ResultInfo UpdateItemWithInfo(string itemId, string info, ConfigInfo configInfo)
        {
            ListItem item = this.list.GetItemById(itemId);
            context.Load(item);
            context.ExecuteQuery();
            try
            {
                item[configInfo.ColumnName] = info;
                item.SystemUpdate();
                Console.WriteLine(item.Id);
                this.context.ExecuteQuery();
                ResultInfo result = new ResultInfo(item.Context.Url, item.Id.ToString(), "Success", "");
                Program.logger.Info("Update item successful. Item Id: {0}.", item.Id);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Warning: Update error. item id: {0}", e.Message));
                Program.logger.Warn("Update item failed. Item Id: {0}.", item.Id);
                ResultInfo result = new ResultInfo(item.Context.Url, item.Id.ToString(), "Error", e.Message);
                return result;
            }
        }

        internal static string RetrieveItemsCombinNewVlaue(ConfigInfo config, List<HPResultInfo> infos, List list)
        {
            List<HPResultInfo> infosCopy = new List<HPResultInfo>();
            foreach (HPResultInfo info in infos)
            {
                CamlQuery query = new CamlQuery() { };
                query.ViewXml = "<View Scope=\"RecursiveAll\"> " +
                    "<Query>" +
                    "<Where>" +
                                "<Eq>" +
                                    "<FieldRef Name=\"" + config.PrimaryKey + "\" />" +
                                    "<Value Type=\"Text\">" + info.HPId + "</Value>" +
                                 "</Eq>" +
                    "</Where>" +
                    "</Query>" +
                    "</View>";
                ListItemCollection items = list.GetItems(query);
                list.Context.Load(items);
                list.Context.ExecuteQuery();
                info.Link = list.Context.Url.TrimEnd('/') + "/" + list.Title + @"/Forms/DispForm.aspx?ID=" + items[0].Id;
                info.Title = items[0]["FileLeafRef"].ToString();
                infosCopy.Add(info);
            }
            Dictionary<string, string> types = new Dictionary<string, string>();
            foreach (HPResultInfo info in infosCopy)
            {
                if (types.ContainsKey(info.RelateType))
                {
                    types[info.RelateType] = types[info.RelateType] + ";" + string.Format("<a href=''{0}''>{1}</a>", info.Link, info.Title);
                }
                else
                {
                    types.Add(info.RelateType, string.Format("{0} :<a href=''{1}''>{2}</a>", info.RelateType, info.Link, info.Title));
                }
            }
            string reslut = "";
            foreach (KeyValuePair<string, string> value in types)
            {
                reslut = reslut + value.Value.ToString() + "<br/>";
            }
            return reslut;
        }
    }
    class ScanItemResult
    {
        public string ItemUrl;
        public string HPTrimId;
        public string OriginalValue;
        public string ItemId;
    }
}
