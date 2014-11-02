﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.IO;

namespace kkot.LzTimer
{
    public class MemoryPeriodStorage : TestablePeriodStorage
    {
        private SortedSet<Period> periods = new SortedSet<Period>();

        public void Remove(Period period)
        {
            periods.Remove(period);
        }

        public SortedSet<Period> GetAll()
        {
            return periods;
        }

        public void Add(Period period)
        {
            periods.Add(period);
        }

        public SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod searchedTimePeriod)
        {
            return new SortedSet<Period>(periods.Where(p =>
                p.End > searchedTimePeriod.Start &&
                p.Start < searchedTimePeriod.End));
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            return new SortedSet<Period>(periods.Where(p =>
                p.End > dateTime));
        }

        public List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            DateTime fromDate = periods.Where((p) => p.Start < dateTime).ToList().Last().Start;
            return periods.Where((p) => p.Start >= fromDate).ToList();
        }

        public void Dispose()
        {
            periods = null;
        }

        public void Reset()
        {
            periods.Clear();
        }
    }

    public class SqlitePeriodStorage : TestablePeriodStorage
    {
        private readonly SQLiteConnection conn;

        public SqlitePeriodStorage(String name)
        {
            if (!File.Exists(name))
            {
                SQLiteConnection.CreateFile(name);
            }

            //conn = new SQLiteConnection("Data Source=" + name + ";Synchronous=Full");
            conn = new SQLiteConnection(String.Format("Data Source={0}",name));
            conn.Open();
            CreateTable();
            PragmaExlusiveAccess();
        }

        public void Add(Period period)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Periods (start, end, type) VALUES (:start, :end, :type)";
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            command.Parameters.AddWithValue("type", period is ActivePeriod ? "A" : "I");
            command.ExecuteNonQuery();
        }

        private void PragmaExlusiveAccess()
        {
            ExecuteNonQuery("PRAGMA locking_mode=EXCLUSIVE");
        }

        private void CreateTable()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Periods (start, end, type)");
        }

        private void ExecuteNonQuery(String sql)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        public void Remove(Period period)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods WHERE start = :start AND end = :end AND type = :type";
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            command.Parameters.AddWithValue("type", period is ActivePeriod ? "A" : "I");
            command.ExecuteNonQuery();
        }

        public SortedSet<Period> GetAll()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT start, end, type FROM Periods", conn);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod searchedTimePeriod)
        {
            var sql = "SELECT start, end, type " +
                      "FROM Periods " +
                      "WHERE end > :start AND start < :end ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", searchedTimePeriod.Start);
            command.Parameters.AddWithValue("end", searchedTimePeriod.End);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            var sql = "SELECT start, end, type " +
                "FROM Periods " +
                "WHERE end > :start ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", dateTime);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        private static Period CreatePeriodFromReader(SQLiteDataReader reader)
        {
            Period period;
            var start = reader["start"].ToString();
            var end = reader["end"].ToString();
            var type = reader["type"].ToString();
            if (type == "A")
            {
                period = new ActivePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            else
            {
                period = new IdlePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            return period;
        }

        public void Dispose()
        {
            conn.Close();
        }

        public void Reset()
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods";
            command.ExecuteNonQuery();
        }
    }
}