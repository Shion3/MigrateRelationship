using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateRelationship
{
    class Constants
    {
        public static readonly string SourceTableTitle = "source";
        public static readonly string JobTableTitle = "JobTable";
        public static readonly string OriginalTableTitle = "OriginalTable";
        //public static readonly string UpdateTableTitle = "UpdateTable";
        public static readonly string ReportTableTitle = "ReportTable";
        public static readonly string CreateJobTable = "create table JobTable(JobId varchar(255),StartTime datetime,EndTime datetime,JobType varchar(255))";
        public static readonly string CreateOriginalTable = "create table OriginalTable(JobId varchar(255),HPTrimID varchar(255),ItemUrl varchar(255),SiteUrl varchar(255),ListTitle varchar(255),OriginalRelatedValue varchar(Max),RelatedValue varchar(Max),ItemId varchar(255))";
        //public static readonly string CreateUpdateTable = "create table UpdateTable(JobId varchar(255),HPTrimID varchar(255),ItemUrl varchar(255),SiteUrl varchar(255),ListTitle varchar(255),OriginalRelatedValue varchar(255),RelatedValue varchar(255),ItemId varchar(255))";
        public static readonly string CreateReportTable = "create table ReportTable(ScanJobId varchar(255),JobId varchar(255),ListUrl varchar(255),ItemId varchar(255),Result varchar(255),Message varchar(255))";
    }
}
