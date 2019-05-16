using System;
using System.Data;
using System.Data.SqlClient;

namespace Corm
{
    /*
     * CORM -- A Simple ORM Framework for C# and SqlServer
     * Author : Ericwyn
     * Github: https://github.com/Ericwyn/Corm
     * CreateDate: 2019/05/16
     */
    public class Corm
    {
        public SqlConnection _sqlConnection { get; }
        public string ConnectStr { get; }

        public Corm(string connectionStr)
        {
            this.ConnectStr = connectionStr;
            this._sqlConnection = new SqlConnection(this.ConnectStr);
            // 直接打开连接
            this._sqlConnection.Open();
        }
        
        // 底层的 sql 执行方法，返回DataSet
        public DataSet queryForDateSet(string sqlStr)
        {
            SqlDataAdapter sqlDataAdapter1 = new SqlDataAdapter(sqlStr, _sqlConnection);//利用已创建好的sqlConnection1,创建数据适配器sqlDataAdapter1
            DataSet dataSetRes = new DataSet();  //创建数据集对象
            sqlDataAdapter1.Fill(dataSetRes);    //执行查询,查询的结果存放在数据集里
            return dataSetRes;
        }
        
        // 
    }
}