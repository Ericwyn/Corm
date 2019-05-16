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
        public string studentAge { get; set; }
    }
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            var corm = new Corm("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount");
            var studentTable = new CormTable<Student>(corm, new Student(), "student");
            studentTable.findAll().Commit();
        }
    }
}