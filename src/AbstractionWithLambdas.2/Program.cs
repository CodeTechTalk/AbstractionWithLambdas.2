using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace AbstractionWithLambdas {
  internal static class Program {
    
    // This is still BAD PRACTICE.
    private const string DbName = "db";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public static void Main(string[] args) {
      Log.Info("Starting application...");
      ListHeroes();
      Log.Info("Application complete.");
    }
    
    // See Schema.sql and Data.sql for info on creating
    // Tables for this demo.
    
    public static void ListHeroes() {
      Log.Info("Selecting and retrieving data...");
      var data = GetData("SELECT Id, Value, Computed, Name, Universe, Appeared, Year FROM dbo.LargeListWithCompute")
                  .ToList();

      Log.Info("Serializing json...");
      var json = JsonConvert.SerializeObject(data);

      Log.Info("Writing file...");
      File.WriteAllText("heroes.out.txt", json);
    }

    /****************************************
     * Basic SQL Query Methods from Video 1 *
     ****************************************/
    
    private static IEnumerable<Dictionary<string, object>> GetData(string query) {
      var data = new List<Dictionary<string, object>>();

      ExecuteQuery(query, reader => {
        
        while (reader.Read()) {
          var item = new Dictionary<string, object>();
          for (var i = 0; i < reader.FieldCount; i++) {
            item.Add(reader.GetName(i), reader[i]);
          }
          data.Add(item);
        }
      });

      return data;
    }

    private static void HandleException(Exception ex) {
      Log.Error($"EXCEPTION: {ex.GetType().Name} :: {ex.Message}");
      Log.Error(ex);
    }
    
    private static void CreateConnection(Action<SqlConnection> action, Action<Exception> except = null) {
      try {

        except = except ?? HandleException;
        
        using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[DbName].ConnectionString)) {
          conn.Open();
          action?.Invoke(conn);
        }
        
      } catch (Exception ex) {
        except?.Invoke(ex);
      }
    }

    private static void CreateCommand(Action<SqlCommand> action, Action<Exception> except = null) {
      CreateConnection(conn => {
        using (var cmd = new SqlCommand {Connection = conn}) {
          action?.Invoke(cmd);
        }
      },except);
    }

    private static void ExecuteQuery(string query, Action<SqlDataReader> action, Action<Exception> except = null) {
      CreateCommand(cmd => {
        cmd.CommandText = query;
        using (var reader = cmd.ExecuteReader()) {
          action?.Invoke(reader);
        }
      },except);
    }

  }
}