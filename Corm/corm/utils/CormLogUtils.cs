namespace CORM.utils
{
    public class CormLogUtils
    {
        private CormSqlPrintCB SqlPrintCb;

        public CormLogUtils(CormSqlPrintCB cb)
        {
            this.SqlPrintCb = cb;
        }
        public void SqlPrint(string logMsg)
        {
            SqlPrintCb.SqlPrint(logMsg);
        }
    }
    
    /*
     * 回调，方便用户自定义 Sql 打印方法
     */
    public interface CormSqlPrintCB
    {
        void SqlPrint(string sql);
    }
}