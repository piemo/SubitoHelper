using SubitoNotifier.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SubitoNotifier.Helper
{
    public static class SQLHelper
    {
        public static LatestInsertion GetLatestInsertionID(string parameters)
        {
            string connStr = ConfigurationManager.ConnectionStrings["SubitoNotifier"].ToString();
            LatestInsertion latestInsertion = null;
            var script = $"select top(1) id, subitoId from recentProducts_tb where parameters = '{parameters}'";

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(script, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(latestInsertion == null)
                                latestInsertion = new LatestInsertion();
                            latestInsertion.Id = reader.GetInt32(0);
                            latestInsertion.SubitoId = reader.GetInt32(1);
                        }
                    }
                }
            }
            return latestInsertion;
        }

        public static LatestInsertion InsertLatestInsertion(int fisrtId, string parameters)
        {
            string connStr = ConfigurationManager.ConnectionStrings["SubitoNotifier"].ToString();
            LatestInsertion latestInsertion = new LatestInsertion();
            CultureInfo info = new CultureInfo("en-US");
            DateTime now = DateTime.Now;
            var script = $"insert into recentProducts_tb(subitoId, parameters, insertedAt) values({fisrtId}, '{parameters}', CONVERT(datetime, '{now.ToString(info)}', 101))";

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(script, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            return latestInsertion;
        }

        public static LatestInsertion UpdateLatestInsertion(LatestInsertion newLatestInsertion)
        {
            string connStr = ConfigurationManager.ConnectionStrings["SubitoNotifier"].ToString();
            LatestInsertion latestInsertion = new LatestInsertion();
            CultureInfo info = new CultureInfo("en-US");
            DateTime now = DateTime.Now;
            var script = $"update recentProducts_tb set SubitoID = {newLatestInsertion.SubitoId}, insertedAt = CONVERT(datetime, '{now.ToString(info)}', 101) where id = {newLatestInsertion.Id}";

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(script, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            return latestInsertion;
        }
    }
}