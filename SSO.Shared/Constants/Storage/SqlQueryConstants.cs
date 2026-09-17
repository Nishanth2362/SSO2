using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Common.Constants.Storage
{
    public static class SqlQueryConstants
    {
        public static string OverAllReportQuery() => "SELECT P.Id,P.Name,P.GmfcsLevl,P.Diagnosis,P.FunctionalLevel, " +
                " (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance != 'ABSENT' and PatientId = P.Id and  " +
                " TreatmentDate between @FromDate and @ToDate) as Present, " +
                " (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance = 'ABSENT' and PatientId = P.Id and " +
                " TreatmentDate  between @FromDate and @ToDate) as Absent, " +
                " sum(DT.Sessions) as Sessions  FROM dailyTreatment as DT " +
                " LEFT JOIN patient as P on DT.PatientId = P.Id " +
                " WHERE P.PatientType = 0 and DT.TreatmentDate between @FromDate and @ToDate   group by DT.PatientId Order by P.Name;" +

                "SELECT P.Id,P.Name,P.GmfcsLevl,P.Diagnosis,P.FunctionalLevel, " +
                "  (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance != 'ABSENT' and PatientId = P.Id and " +
                "  TreatmentDate between @FromDate and @ToDate) as Present, " +
                "  (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance = 'ABSENT' and PatientId = P.Id and " +
                "  TreatmentDate  between @FromDate and @ToDate) as Absent, " +
                "  (SELECT COALESCE(SUM(Sessions), 0) FROM dailyTreatment WHERE PatientId = P.Id and " +
                "  TreatmentDate  between @FromDate and @ToDate) As Sessions " +
                " FROM patient as P " +
                " WHERE P.PatientType = 0 and P.DateOfVisit <= @ToDate " +
                " and Exists(SELECT Id From dailyTreatment Where PatientId = P.Id and TreatmentDate <= date_add(@ToDate, INTERVAL - 4 Month)) " +
                " and not Exists(SELECT Id From dailyTreatment Where PatientId = P.Id and TreatmentDate between @FromDate and @ToDate) " +
                " and (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance != 'ABSENT' and PatientId = P.Id and " +
                " TreatmentDate between @FromDate and @ToDate) = 0 " +
                " and (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance = 'ABSENT' and PatientId = P.Id and " +
                " TreatmentDate between @FromDate and @ToDate) = 0 ;" +

                "SELECT P.Id,P.Name,P.GmfcsLevl,P.Diagnosis,P.FunctionalLevel, " +
                " (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance != 'ABSENT' and PatientId = P.Id and " +
                " TreatmentDate between @FromDate and @ToDate) as Present, " +
                " (SELECT Count(distinct DailyTreatmentId) FROM dailyPrograms WHERE Attendance = 'ABSENT' and PatientId = P.Id and " +
                " TreatmentDate  between @FromDate and @ToDate) as Absent, " +
                " (SELECT SUM(Sessions) FROM dailyTreatment WHERE PatientId = P.Id and " +
                " TreatmentDate between @FromDate and @ToDate) As Sessions " +
                " FROM patient as P WHERE P.PatientType = 0 and P.DateOfVisit between @FromDate and @ToDate " +
                " group by P.Id Order by P.DateOfVisit asc; " +

                "";
        public static string DashBoardQuery() => $"SELECT P.Name as Name ,P.PatientType as PatientType, DT.TreatmentDate AssesmentDate,DT.Sessions as Sessions," +
            $" (SELECT count(Id) from dailyPrograms WHERE DailyTreatmentId=DT.Id and Attendance!='ABSENT' and Program != 'INITIAL VISIT') as Activities," +
            $" case when P.PictureUrl is null then '' else   SUBSTRING_INDEX(P.PictureUrl,'\',-1) end as  PicUrl " +
            $" from dailyTreatment as DT " +
            $" INNER JOIN patient as P on DT.PatientId = P.Id " +
            $" order by DT.TreatmentDate limit 20; " +


            $" Select count( distinct( DT.PatientId)) as Count, month( DT.TreatmentDate) as Month ,year(DT.TreatmentDate) as Year,P.PatientType as PatientType from dailyTreatment as DT " +
            $"    join patient as P on DT.PatientId =P.Id " +
            $"        Where P.PatientType=0 " +
            $"          and exists (select DP.Id from dailyPrograms as DP where DP.Attendance!='ABSENT' and DP.Attendance!='INITIAL VISIT' and  DP.DailyTreatmentId  = DT.Id) " +
            $"          and  DT.TreatmentDate  between  @FromDate and @ToDate" +
            $"        Group by month( DT.TreatmentDate),year(DT.TreatmentDate) " +
            $"        order by DT.TreatmentDate limit 12;" +

            $" Select count( distinct( DT.PatientId)) as Count, month( DT.TreatmentDate) as Month ,year(DT.TreatmentDate) as Year,P.PatientType as PatientType from dailyTreatment as DT " +
            $"    join patient as P on DT.PatientId =P.Id " +
            $"        Where P.PatientType=1 " +
            $"          and exists(select DP.Id from dailyPrograms as DP where DP.Attendance!= 'ABSENT' and DP.Attendance!= 'INITIAL VISIT' and DP.DailyTreatmentId  = DT.Id) " +
            $"          and  DT.TreatmentDate  between  @FromDate and @ToDate" +
            $"        Group by month( DT.TreatmentDate),year(DT.TreatmentDate) " +
            $"        order by DT.TreatmentDate limit 12;" +

            $" Select case when  (GmfcsLevl is null or GmfcsLevl ='') then 'Not Registered' else GmfcsLevl end as label, " +
            $"     count(id) as value from patient where PatientType=0  group by GmfcsLevl;" +

             $" Select case when  (GmfcsLevl is null or GmfcsLevl ='') then 'Not Registered' else GmfcsLevl end as label, " +
            $"     count(id) as value from patient where PatientType=1 group by GmfcsLevl;";


    }
}
