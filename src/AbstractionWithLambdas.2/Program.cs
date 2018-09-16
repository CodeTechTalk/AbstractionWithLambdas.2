using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace AbstractionWithLambdas {
  internal static class Program {
    
    // This is still BAD PRACTICE.
    private const string DbName = "db";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public static void Main(string[] args) {
      Log.Info("Starting application...");
      Time(ListHeroesDictionary);
      Time(ListHeroesDataTable);
      Time(ListHeroEntries);
      Time(ListDataGeneric);
      Log.Info("Application complete.");
    }
    
    // See Schema.sql and Data.sql for info on creating
    // Tables for this demo.

    public static void Time(Action action) {
      var sw = new Stopwatch();
      sw.Start();
      action();
      sw.Stop();
      Log.Trace($"Process completed in {sw.ElapsedMilliseconds}ms");
    }
    
    public static void ListHeroesDataTable() {
      Log.Info("Starting Data Table Process...");
      Log.Info("Selecting and retrieving data...");
      var data = GetDataTable(
        "SELECT Id, Value, Computed, Name, Universe, Appeared, Year FROM dbo.LargeListWithCompute");

      Log.Info("Serializing json...");
      var json = JsonConvert.SerializeObject(data);

      Log.Info("Writing file...");
      File.WriteAllText("heroes.dt.out.txt", json);
    }
    
    public static void ListHeroesDictionary() {
      Log.Info("Starting Dictionary Process...");
      Log.Info("Selecting and retrieving data...");
      var data = GetData("SELECT Id, Value, Computed, Name, Universe, Appeared, Year FROM dbo.LargeListWithCompute")
                  .ToList();

      Log.Info("Serializing json...");
      var json = JsonConvert.SerializeObject(data);

      Log.Info("Writing file...");
      File.WriteAllText("heroes.dict.out.txt", json);
    }
    
    public static void ListHeroEntries() {
      Log.Info("Starting Dictionary Process...");
      Log.Info("Selecting and retrieving data...");
      var data = GetHeroEntries("SELECT Id, Value, Computed, Name, Universe, Appeared, Year FROM dbo.LargeListWithCompute")
                  .ToList();

      Log.Info("Serializing json...");
      var json = JsonConvert.SerializeObject(data);

      Log.Info("Writing file...");
      File.WriteAllText("heroes.obj.out.txt", json);
    }
    
    public static void ListDataGeneric() {
      Log.Info("Starting Dictionary Process...");
      Log.Info("Selecting and retrieving data...");
      var data = GetData<HeroEntry>("SELECT Id, Value, Computed, Name, Universe, Appeared, Year FROM dbo.LargeListWithCompute")
                  .ToList();

      Log.Info("Serializing json...");
      var json = JsonConvert.SerializeObject(data);

      Log.Info("Writing file...");
      File.WriteAllText("heroes.generic.out.txt", json);
    }
    
    private static DataTable GetDataTable(string query) {
      var dt = new DataTable();

      ExecuteQuery(query, reader => dt.Load(reader));

      return dt;
    }

    private static string GetStr(IDataReader reader, string field) {
      return reader[field] is DBNull
        ? null
        : reader[field].ToString();
    }

    private static int GetInt(IDataReader reader, string field) {
      var obj = reader[field];
      if (obj is DBNull) {
        return default(int);
      }

      var str = obj.ToString();
      if (string.IsNullOrWhiteSpace(str)) {
        return default(int);
      }

      return Convert.ToInt32(str);
    }
    
    private static IEnumerable<HeroEntry> GetHeroEntries(string query) {
      var data = new List<HeroEntry>();

      ExecuteQuery(query, reader => {
        
        while (reader.Read()) {
          var item = new HeroEntry {
            Name = GetStr(reader, "Name"),
            Appeared = GetInt(reader, "Appeared"),
            Computed = GetInt(reader, "Computed"),
            Id = GetInt(reader, "Id"),
            Universe = GetStr(reader, "Universe"),
            Value = GetStr(reader, "Value"),
            Year = GetInt(reader, "Year")
          };
        }
      });

      return data;
    }
    
    private static IEnumerable<T> GetData<T>(string query) where T : new() {
      var data = new List<T>();
      var type = typeof(T);

      ExecuteQuery(query, reader => {
        
        while (reader.Read()) {
          var obj = Activator.CreateInstance<T>();

          for (var i = 0; i < reader.FieldCount; i++) {
            var field = reader.GetName(i);

            var prop = type.GetProperty(field, BindingFlags.Public);

            if (prop == null) {
              continue;
            }

            var value = reader[i];
            if (!prop.PropertyType.IsInstanceOfType(value)) {
              continue;
            }
            
            prop.SetValue(obj,value);

          }

          data.Add(obj);

        }
        
      });

      return data;
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