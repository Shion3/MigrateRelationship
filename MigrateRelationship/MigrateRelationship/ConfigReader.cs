using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MigrateRelationship
{
    class ConfigInfo
    {
        public string SPSiteUrl;
        public string SPListTitle;
        public string SPUserName;
        public string SPPassWord;
        public string DBServer;
        public string DBDatabaseName;
        public bool DBWindowsMode;
        public string DBUserName;
        public string DBPassword;
        public bool JobType;
        public string PrimaryKey;
        public string ColumnName;
        public ConfigInfo(string siteUrl, string listTitle, string spUserName, string spPassword, string dbServer, string dbDatabaseName, bool windowsMode, string dbUsername, string dbPassword, bool jobType,string primaryKey,string columnName)
        {
            this.SPSiteUrl = siteUrl;
            this.SPListTitle = listTitle;
            this.SPUserName = spUserName;
            this.SPPassWord = spPassword;
            this.DBServer = dbServer;
            this.DBDatabaseName = dbDatabaseName;
            this.DBWindowsMode = windowsMode;
            this.DBUserName = dbUsername;
            this.DBPassword = dbPassword;
            this.JobType = jobType;
            this.PrimaryKey = primaryKey;
            this.ColumnName = columnName;
        }
    }
    class ConfigReader
    {
        private string path;
        private XmlDocument doc;
        private XmlElement ele;
        public ConfigReader(string path)
        {
            this.path = path;
            this.doc = new XmlDocument();
            this.doc.Load(this.path);
            this.ele = this.doc.DocumentElement;
        }
        public ConfigInfo GetConfigInfo()
        {
            return new ConfigInfo(this.ele.SelectSingleNode("/Root/AccountInfo/SiteUrl").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/AccountInfo/ListUrl").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/AccountInfo/UserName").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/AccountInfo/PassWord").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/DatabaseInfo/ServerName").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/DatabaseInfo/DatabaseName").Attributes["Value"].Value,
                bool.Parse(this.ele.SelectSingleNode("/Root/DatabaseInfo/IntegratedWindows").Attributes["Value"].Value),
                this.ele.SelectSingleNode("/Root/DatabaseInfo/UserName").Attributes["Value"].Value,
                this.ele.SelectSingleNode("/Root/DatabaseInfo/PassWord").Attributes["Value"].Value,
            bool.Parse(this.ele.SelectSingleNode("/Root/JobInfo/ScanItemJob").Attributes["Value"].Value),
            this.ele.SelectSingleNode("/Root/SPColumnName/PrimaryKey").Attributes["Value"].Value,
            this.ele.SelectSingleNode("/Root/SPColumnName/ColumnName").Attributes["Value"].Value)
            { };
        }
    }
    enum JobType
    {
        ScanItemJob = 1,
        UpdateItemJob = 2
    }
}
