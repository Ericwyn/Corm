using System.Data;
using System.Xml;
using Corm.attrs;

namespace Corm
{
    public class Student
    {
        [CormColumn(Name = "studentName_", Length = 10,DbType = SqlDbType.VarChar)]
        public string studentName { get; set; }
        [CormColumn(Name = "studentAge_", Length = 2, DbType = SqlDbType.Int)]
        public int studentAge { get; set; }
    }
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            var corm = new Corm("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount");
            var studentTable = new CormTable<Student>(corm, "student");
            // find all
            // studentTable.Find().All().Commit();
            // find where
            var selectTemp = new Student();
//            selectTemp.studentAge = 10;
            selectTemp.studentName = "aaa";
            studentTable.Find().Where(selectTemp).Commit();
        }
    }
}