/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Data.SQLite;

namespace Dec0de.UI.Database
{
    class DatabaseCreator
    {
        public const string DatabaseName = "decodedb.db3";
        private static string _databaseFolder = null;
        public static string DatabasePath;
        public static string DatabaseFolder
        {
            get { return _databaseFolder; }
            set
            {
                _databaseFolder = value;
                DatabasePath = (value == null) ? null : Path.Combine(DatabaseFolder, DatabaseName);
            }
        }

        public static bool Exists()
        {
            return ((DatabasePath != null) && File.Exists(DatabasePath));
        }

        public static void CreateAndInitialize()
        {
            if (Exists()) return;
            SQLiteConnection.CreateFile(DatabasePath);
            bool initialized = false;
            SQLiteConnection sql = null;
            try {
                sql = DatabaseAccess.OpenSql();
                const string stmtA = @"CREATE TABLE tbl_Hash(hashRunId INTEGER, blockIndex INTEGER, hash VARCHAR(200))";
                Execute(sql, stmtA);
                const string stmtB1 = @"CREATE INDEX Indx_Run ON tbl_Hash(hashRunId)";
                Execute(sql, stmtB1);
                const string stmtB2 = @"CREATE INDEX Indx_HashBlock ON tbl_Hash(hash,hashRunId)";
                Execute(sql, stmtB2);
                string stmt;
                stmt = String.Format("CREATE TABLE tbl_HashRun({0},{1},{2},{3},{4},{5},{6},{7},{8},{9})",
                                     "hashRunId INTEGER PRIMARY KEY ASC",
                                     "dateTime DATETIME",
                                     "phoneId INTEGER",
                                     "memoryId VARCHAR(50)",
                                     "slideAmount INTEGER",
                                     "blocksizeBytes INTEGER",
                                     "numBlocks INTEGER",
                                     "timeToHashSeconds INTEGER",
                                     "hashType VARCHAR(100)",
                                     "notes VARCHAR(1024)");
                Execute(sql, stmt);
                const string stmtC = @"CREATE INDEX Indx_MemoryBlocksSlide ON tbl_HashRun(memoryId,blocksizeBytes,slideAmount)";
                Execute(sql, stmtC);
                const string stmtD1 = @"CREATE INDEX Indx_PhoneId ON tbl_HashRun(phoneId)";
                Execute(sql, stmtD1);
                const string stmtD2 = @"CREATE INDEX Indx_BlockSize ON tbl_HashRun(blocksizeBytes)";
                Execute(sql, stmtD2);
                const string stmtD3 = @"CREATE INDEX Indx_SlideAmount ON tbl_HashRun(slideAmount)";
                Execute(sql, stmtD3);
                const string stmtE = @"CREATE UNIQUE INDEX Indx_HashrunUnique ON tbl_HashRun(dateTime,phoneId,blocksizeBytes)";
                Execute(sql, stmtE);
                stmt = String.Format("CREATE TABLE tbl_Phone({0},{1},{2},{3},{4})",
                                     "phoneId INTEGER PRIMARY KEY ASC",
                                     "make VARCHAR(50)",
                                     "model VARCHAR(50)",
                                     "subId VARCHAR(50)",
                                     "notes VARCHAR(1024)");
                Execute(sql, stmt);
                const string stmtF = @"CREATE UNIQUE INDEX Indx_PhonePhone ON tbl_Phone(phoneId)";
                Execute(sql, stmtF);
                stmt = String.Format("CREATE TABLE tbl_Constants({0},{1},{2})",
                                     "value VARCHAR(4)",
                                     "blocksizeBytes INTEGER",
                                     "hash VARCHAR(200)");
                Execute(sql, stmt);
                const string stmtG = @"CREATE INDEX Indx_BlockSizeBytes ON tbl_Constants(blocksizeBytes)";
                Execute(sql, stmtG);
                PopulateConstantsAsPhone(sql);
                PopulateConstants(sql);
                initialized = true;
            } catch (Exception ex) {
                initialized = false;
                throw ex;
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                        sql.Dispose();
                    } catch {
                    }
                }
                if (!initialized) {
                    try {
                        File.Delete(DatabasePath);
                    } catch {
                    }
                }
            }
        }

        private static void Execute(SQLiteConnection sql, string stmt)
        {
            SQLiteCommand cmd = sql.CreateCommand();
            cmd.CommandText = stmt;
            cmd.ExecuteNonQuery();
        }

        private static void ExecuteIgnore(SQLiteConnection sql, string stmt)
        {
            try {
                SQLiteCommand cmd = sql.CreateCommand();
                cmd.CommandText = stmt;
                cmd.ExecuteNonQuery();
            } catch (Exception) {
            }
        }

        /// <summary>
        /// Adds constants to tbl_Hash if not already there due to the DB
        /// being created by an older version of the program.
        /// </summary>
        public static void PopulateConstantsMigrate()
        {
            if (Exists()) {
                SQLiteConnection sqlx = null;
                try {
                    sqlx = DatabaseAccess.OpenSql();
                    PopulateConstantsAsPhone(sqlx);
                } catch {
                } finally {
                    if (sqlx != null) sqlx.Close();
                }
            }
        }

        /// <summary>
        /// Rather than use a separate table for constant hashes, insert the
        /// hashes into the phone block hash tales, using a phone id of 0.
        /// </summary>
        /// <param name="sql">Connection to the database.</param>
        private static void PopulateConstantsAsPhone(SQLiteConnection sql)
        {
            // Have the constants already been inserted. We do this check
            // because the DB may have been created by an older version of
            // dec0de.
            SQLiteDataReader rdr = null;
            try {
                const string stmt = "SELECT hashRunId FROM tbl_Hash WHERE hashRunId=0 LIMIT 1";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                rdr = cmd.ExecuteReader();
                if (rdr.HasRows) return;
            } catch (Exception) {
                return;
            } finally {
                if (rdr != null) rdr.Close();
            }

            // Insert the constant hashes.
            try {
                int[] blockSizes =
                    {
                        HashLoader.HashLoader.DefaultBlockSize
                    };
                int maxBlockSize = blockSizes[blockSizes.Length - 1];
                const string stmt = "INSERT INTO tbl_Hash(hashRunId, blockIndex, hash) " +
                                    "VALUES(0, @BX, @HASH)";
                SHA1 provider = new SHA1CryptoServiceProvider();
                SQLiteTransaction trans = sql.BeginTransaction();
                int bx = 0;
                for (int n = 0; n <= 0xff; n++) {
                    byte[] bytes = new byte[maxBlockSize];
                    for (int j = 0; j < maxBlockSize; j++) bytes[j] = (byte)n;
                    foreach (int size in blockSizes) {
                        byte[] hash = provider.ComputeHash(bytes, 0, size);
                        SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                        cmd.Parameters.AddWithValue("@BX", ++bx);
                        cmd.Parameters.AddWithValue("@HASH", Convert.ToBase64String(hash));
                        cmd.ExecuteNonQuery();
                    }
                }
                trans.Commit();
                trans.Dispose();
            } catch (Exception) {
            }
        }

        private static void PopulateConstants(SQLiteConnection sql)
        {
            try {
                int[] blockSizes =
                    {
                        //128,
                        //256,
                        //512,
                        1024,
                        //2048,
                        //4096,
                        //8192,
                        //16384,
                        //32768
                    };
                const string stmt = "INSERT INTO tbl_Constants(value, blocksizeBytes, hash) " +
                                    "VALUES(@VAL, @BSB, @HASH)";
                SHA1 provider = new SHA1CryptoServiceProvider();
                SQLiteTransaction trans = sql.BeginTransaction();
                for (int n = 0; n <= 0xff; n++) {
                    byte[] bytes = new byte[1024];
                    for (int j = 0; j < 1024; j++) bytes[j] = (byte)n;
                    foreach (int size in blockSizes) {                      
                        byte[] hash = provider.ComputeHash(bytes, 0, size);
                        SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                        cmd.Parameters.AddWithValue("@VAL", String.Format("0x{0:x2}", n));
                        cmd.Parameters.AddWithValue("@BSB", size);
                        cmd.Parameters.AddWithValue("@HASH", Convert.ToBase64String(hash));
                        cmd.ExecuteNonQuery();
                    }
                }
                trans.Commit();
                trans.Dispose();
            } catch (Exception) {
            }
        }

    }
}
