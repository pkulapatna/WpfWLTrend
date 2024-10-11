using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WpfWLTrend.Properties;

namespace WpfWLTrend
{
    public class ClsSqlConfig
    {
        public SqlConnection SqlConnection { get; set; }
        
        public string m_CurrentWLTable { get; set; }
        public string m_PreviousWLTable { get; set; }

        public string DataSource { get; set; }

        //Work Stations
        public string LocalWorkStationID { get; set; }

        public string TargetWorkStationID { get; set; }
        //Connection strings
        public string m_ConString { get; set; }
        public string m_WLConStr { get; set; }
        public string m_MasterWLConStr { get; set; }
        public string strRemoteMasterCon { get; set; }
        private string m_strMasterCon { get; set; }
        public string m_strUserName { get; set; }
        public string m_strPassWrd { get; set; }
        public string m_strDatabase { get; set; }
        public string m_strWLDatabase { get; set; }

        public string m_strHostID { get; set; }
        public string m_strInstance { get; set; }

        public const int BALE_ARCHIVE = 0;
        public const int LOT_ARCHIVE = 1;
        public const int UNIT_ARCHIVE = 2;
        public const int WET_LAYER = 3;
        public const int QULITY_ARCHIVE = 4;

        public List<string> WLTableList;

        public List<string> m_LineList { get; set; }
        public List<string> m_SourceList { get; set; }


        public ClsSqlConfig()
        {
            WLTableList = new List<string>();
            SetSqlParams();
            SetConnectionString();

        }

        internal List<string> GetAllWLTables()
        {
            WLTableList = GettableList(WET_LAYER);
            return WLTableList;
        }

        private List<string> GettableList(int wET_LAYER)
        {
            List<string> tablelist = new List<string>();
            string strquery = "SELECT [name],create_date FROM sys.tables WHERE [name] LIKE '%FValueReadings%' ORDER BY create_date DESC";
            
            try
            {
                using (var sqlConnection = new SqlConnection(m_MasterWLConStr))
                {
                    if (sqlConnection != null) sqlConnection.Open();
                    using (var command = new SqlCommand(strquery, sqlConnection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                                while (reader.Read())
                                {
                                    if (reader.GetString(0) != null)
                                        tablelist.Add(reader.GetString(0));
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GettableList -> " + ex.Message);  
            }
            return tablelist;
        }

        internal List<string> GetUniqueStrItemlist(string strItem, string strTable)
        {

            string constr = m_ConString;
            List<string> itemList = new List<string>();
            string strQuery = "SELECT DISTINCT " + strItem + " From " + strTable + " ORDER BY " + strItem + ";";

            if (strItem == "BalerID") constr = m_MasterWLConStr;

            try
            {
                using (var sqlConnection = new SqlConnection(constr))
                {
                    if (sqlConnection != null) sqlConnection.Open();
                    using (var command = new SqlCommand(strQuery, sqlConnection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                                while (reader.Read())
                                {
                                    if (reader != null)
                                        itemList.Add(reader[0].ToString());
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetUniqueStrItemlist " + ex.Message);
            }
            return itemList;
        }

        internal DataTable GetNewWLDataTable(object selectedValue, string strQuery)
        {
            DataTable mytable = new DataTable();

            try
            {
                using (var sqlConnection = new SqlConnection(m_MasterWLConStr))
                {
                    using (var adapter = new SqlDataAdapter(strQuery, sqlConnection))
                    {
                        adapter.Fill(mytable);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GettableList -> " + ex.Message);
            }
            return mytable;
        }

        private void SetConnectionString()
        {
            string strWorkStation = LocalWorkStationID + @"\" + m_strInstance;

            //Forte Realtime
            m_ConString = "Data Source ='" + strWorkStation + "'; Database = "
                + m_strDatabase + "; User id= '" + m_strUserName + "'; Password = '"
                + m_strPassWrd + "'; connection timeout=30;Persist Security Info=True;";

            //ForteLayer
            m_WLConStr = "Data Source ='" + strWorkStation + "'; Database = "
                + m_strWLDatabase + "; User id= '" + m_strUserName + "'; Password = '"
                + m_strPassWrd + "'; connection timeout=30;Persist Security Info=True;";

            //For Wetlayer Local machine
            m_MasterWLConStr = "workstation id=" + Environment.MachineName
                + ";packet size=4096;integrated security=SSPI;data source='"
                + strWorkStation + "';persist security info=False;initial catalog= " + m_strWLDatabase; // master";

        }

        private void SetSqlParams()
        {
            LocalWorkStationID = Environment.MachineName;
            Settings.Default.LocalHost = LocalWorkStationID;
            Settings.Default.Save();


            if (Settings.Default.Instance != null)
                m_strInstance = Settings.Default.Instance;
            else
                m_strInstance = "SQLEXPRESS";

            if (Settings.Default.UserName != null)
                m_strUserName = Settings.Default.UserName;
            else
                m_strUserName = "forte";

            if (Settings.Default.PassWord != null)
                m_strPassWrd = Settings.Default.PassWord;
            else
                m_strPassWrd = "etrof";

            if (Settings.Default.Database != null)
                m_strDatabase = Settings.Default.Database;
            else
                m_strDatabase = "fortedata";

            if (Settings.Default.WLDatabase != null)
                m_strWLDatabase = Settings.Default.WLDatabase;
            else
                m_strWLDatabase = "ForteLayer";

        }
    }
}
