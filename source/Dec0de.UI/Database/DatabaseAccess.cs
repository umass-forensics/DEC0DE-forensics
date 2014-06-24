/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;
using System.Threading;
using Dec0de.Bll.Filter;

namespace Dec0de.UI.Database
{

    internal class DatabaseAccess
    {

        public static SQLiteConnection OpenSql(bool readOnly)
        {
            SQLiteConnectionStringBuilder connBuilder = new SQLiteConnectionStringBuilder();
            connBuilder.DataSource = DatabaseCreator.DatabasePath;
            connBuilder.Version = 3;
            connBuilder.PageSize = 4096;
            connBuilder.FailIfMissing = true;
            connBuilder.JournalMode = (readOnly) ? SQLiteJournalModeEnum.Off : SQLiteJournalModeEnum.Default;
            connBuilder.SyncMode = SynchronizationModes.Normal;
            connBuilder.ReadOnly = readOnly;
            connBuilder.Pooling = true;
            SQLiteConnection sql = new SQLiteConnection(connBuilder.ToString());
            sql.Open();
            return sql;
        }

        public static SQLiteConnection OpenSql()
        {
            return OpenSql(false);
        }

        public static SQLiteConnection OpenSqlFastWrite()
        {
            SQLiteConnectionStringBuilder connBuilder = new SQLiteConnectionStringBuilder();
            connBuilder.DataSource = DatabaseCreator.DatabasePath;
            connBuilder.Version = 3;
            connBuilder.FailIfMissing = true;
            connBuilder.JournalMode = SQLiteJournalModeEnum.Off;
            connBuilder.SyncMode = SynchronizationModes.Off;
            SQLiteConnection sql = new SQLiteConnection(connBuilder.ToString());
            sql.Open();
            return sql;
        }

        /// <summary>
        /// Gets the phoneID field for a phone from the table: tbl_HahsRun in the database, given its details.
        /// </summary>
        /// <param name="memoryIdSha1">SHA1 hash of the entire memory file, serving as a memory id for the phone.</param>
        /// <param name="blockSize">The size of blocks used for block hash filtering</param>
        /// <param name="slideAmt">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <returns>The phoneID from the database.</returns>
        public static int GetPhoneidByMemoryid(string memoryIdSha1, int blockSize, int slideAmt)
        {
            SQLiteConnection sql = null;
            try {
                sql = OpenSql();
                const string stmt = "SELECT phoneId FROM tbl_HashRun " +
                                    "WHERE memoryId=@MEMID " +
                                    "AND blocksizeBytes=@BLKSZ " +
                                    "AND slideAmount=@AMT";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@MEMID", memoryIdSha1);
                cmd.Parameters.AddWithValue("@BLKSZ", blockSize);
                cmd.Parameters.AddWithValue("@AMT", slideAmt);
                SQLiteDataReader rdr = null;
                try {
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        return Convert.ToInt32(rdr["phoneId"]);
                    }
                    return -1;
                } finally {
                    if (rdr != null) {
                        rdr.Close();
                    }
                }
            } catch (Exception) {
                return -1;
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                    } catch {
                    }
                }
            }
        }

        /// <summary>
        /// Called to remove references to a phone.
        /// </summary>
        /// <param name="memoryIdSha1"></param>
        /// <param name="phoneId"></param>
        /// <param name="hashRunId"></param>
        public static void ForgetPhone(string memoryIdSha1, int phoneId, int hashRunId)
        {
            SQLiteConnection sql = null;
            try {
                sql = OpenSql();
            } catch {
                return;
            }
            try {
                try {
                    const string stmt = "DELETE FROM tbl_HashRun WHERE memoryId=@MEMID";
                    SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                    cmd.Parameters.AddWithValue("@MEMID", memoryIdSha1);
                    cmd.ExecuteNonQuery();
                } catch {
                }
                try {
                    const string stmt = "DELETE FROM tbl_Phone WHERE phoneId=@PH";
                    SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                    cmd.Parameters.AddWithValue("@PH", phoneId);
                    cmd.ExecuteNonQuery();
                } catch {
                }
                try {
                    const string stmt = "DELETE FROM tbl_Hash WHERE hashRunId=@HR";
                    SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                    cmd.Parameters.AddWithValue("@HR", hashRunId);
                    cmd.ExecuteNonQuery();
                } catch {
                }
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                    } catch {
                    }
                }
            }
        }

        /// <summary>
        /// Inserts in the database, into the table: tbl_Hash, newly found block hashes of the phone.
        /// </summary>
        /// <param name="hashes">List of block hashes of the phone.</param>
        /// <param name="hashRunId">The ID of the row to which the block hashes of the phone are to be inserted.</param>
        /// <returns>The number of block hashes or -1 in case of an exception.</returns>
        public static int InsertHashes(List<string> hashes, int hashRunId, List<BlockHashFilter.UnfilterdBlockResult> stored=null)
        {
            // TODO: This is slow, but we are trying to maintain all-or-none integrity.
            // TODO: Can we do away with this? The concern is more user cancels than crashes.
            // TODO: We don't want a partial phone in the DB because it will either be detected as already there,
            // or hash block fliter itself out! 
            SQLiteConnection sql = null;
            int blockIndex = 0;
            try {
                HashSet<string> loaded = new HashSet<string>();
                sql = OpenSql();
                SQLiteTransaction trans = sql.BeginTransaction();
                const string stmt = "INSERT INTO tbl_Hash(hashRunId, blockIndex, hash) " +
                                    "VALUES(@RUNID, @BLKX, @HASH)";
                foreach (string blkhash in hashes) {
                    // Only add if it's a unique hash.
                    if (!loaded.Contains(blkhash)) {
                        SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                        cmd.Parameters.AddWithValue("@RUNID", hashRunId);
                        cmd.Parameters.AddWithValue("@BLKX", blockIndex);
                        cmd.Parameters.AddWithValue("@HASH", blkhash);
                        cmd.ExecuteNonQuery();
                        if (stored != null) {
                            BlockHashFilter.UnfilterdBlockResult ubr = new BlockHashFilter.UnfilterdBlockResult
                                                                           {
                                                                               blockIndexFirst = blockIndex,
                                                                               hash = blkhash
                                                                           };
                            stored.Add(ubr);
                        }
                        loaded.Add(blkhash);
                    }
                    blockIndex++;
                }
                loaded.Clear();
                trans.Commit();
                return blockIndex;
            } catch (ThreadAbortException e) {
                throw e;
            } catch (Exception) {
                return -1;
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                        sql.Dispose();
                    } catch {
                    }
                }
            }
        }

        /// <summary>
        /// Updates the table: tbl_HashRun, by udating the time to hash and number of blocks fields of the row to which new block hashes have been added.
        /// </summary>
        /// <param name="hashRunId">The ID of the row to which the block hashes of the phone are to be inserted.</param>
        /// <param name="numBlocks">The total number of blocks hashed for this phone.</param>
        /// <param name="timeToHashSeconds">The time taken to perform block hashing for this phone.</param>
        public static void HashRunUpdate(int hashRunId, int numBlocks, int timeToHashSeconds)
        {
            SQLiteConnection sql = null;
            try {
                sql = OpenSql();
                const string stmt = "UPDATE tbl_HashRun " +
                                    "SET numBlocks=@NBLKS, timeToHashSeconds=@SECS " +
                                    "WHERE hashRunId=@RUNID";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@RUNID", hashRunId);
                cmd.Parameters.AddWithValue("@NBLKS", numBlocks);
                cmd.Parameters.AddWithValue("@SECS", timeToHashSeconds);
                cmd.ExecuteNonQuery();
            } catch (Exception) {
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                        sql.Dispose();
                    } catch {
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new row for the phone's general details in the database, into a table: tbl_HashRun.
        /// </summary>
        /// <param name="dateTime">The present data/time.</param>
        /// <param name="phoneId">The unique identification for this phone in the database.</param>
        /// <param name="memoryId">The FileSHA1 of the memory file.</param>
        /// <param name="slideAmount">The distance between the starting offsets of two consecutive blocks in memory.</param>
        /// <param name="blockSize">The size of each block in phone's memory.</param>
        /// <param name="numBlocks">The total number of blocks in phone's memory.</param>
        /// <param name="timeToHashSeconds">Time taken to calculate all block hashes of the phone.</param>
        /// <param name="hashType">The kind of hash, for example SHA1.</param>
        /// <param name="notes">General information regarding the phone.</param>
        /// <returns>The row ID of the record just inserted for this phone in the table.</returns>
        public static int HashRunInsert(DateTime dateTime, int phoneId, string memoryId, int slideAmount,
                                         int blockSize, int numBlocks, int timeToHashSeconds, string hashType,
                                         string notes)
        {
            SQLiteConnection sql = null;
            try {
                sql = OpenSql();
                try {
                    // Default is 2000 1K pages, this should help?
                    (new SQLiteCommand("PRAGMA cache_size=8000", sql)).ExecuteNonQuery();
                } catch {
                }
                const string stmt = "INSERT INTO tbl_HashRun(dateTime,phoneId," +
                                    "memoryId,slideAmount,blocksizeBytes,numBlocks," +
                                    "timeToHashSeconds,hashType,notes) " +
                                    "VALUES(@DT,@PID,@MID,@SA,@BSB,@NB,@TTH,@HT,@NOTES)";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@DT", dateTime);
                cmd.Parameters.AddWithValue("@PID", phoneId);
                cmd.Parameters.AddWithValue("@MID", memoryId);
                cmd.Parameters.AddWithValue("@SA", slideAmount);
                cmd.Parameters.AddWithValue("@BSB", blockSize);
                cmd.Parameters.AddWithValue("@NB", numBlocks);
                cmd.Parameters.AddWithValue("@TTH", timeToHashSeconds);
                cmd.Parameters.AddWithValue("@HT", hashType);
                cmd.Parameters.AddWithValue("@NOTES", notes);
                cmd.ExecuteNonQuery();
                return (int)sql.LastInsertRowId;
            } catch (Exception) {
                return -1;
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                        sql.Dispose();
                    } catch {
                    }
                }
            }
        }

        /// <summary>
        /// Inserts into the table: tblPhone in the database details such as phone's model, make, notes etc.
        /// </summary>
        /// <param name="make">Make of the phone.</param>
        /// <param name="model">Model of the phone.</param>
        /// <param name="subId">A globally unique identifier for the record.</param>
        /// <param name="notes">General information regarding the phone.</param>
        /// <returns>The row ID of the record just inserted for this phone in the table.</returns>
        public static int PhoneInsert(string make, string model, string subId, string notes)
        {
            SQLiteConnection sql = null;
            try {
                sql = OpenSql();
                const string stmt = "INSERT INTO tbl_Phone(make,model,subId,notes) " +
                                    "VALUES(@MAKE,@MODEL,@SID,@NOTES)";
                SQLiteCommand cmd = new SQLiteCommand(stmt, sql);
                cmd.Parameters.AddWithValue("@MAKE", make);
                cmd.Parameters.AddWithValue("@MODEL", model);
                cmd.Parameters.AddWithValue("@SID", subId);
                cmd.Parameters.AddWithValue("@NOTES", notes);
                cmd.ExecuteNonQuery();
                return (int)sql.LastInsertRowId;
            } catch (Exception) {
                return -1;
            } finally {
                if (sql != null) {
                    try {
                        sql.Close();
                        sql.Dispose();
                    } catch {
                    }
                }
            }
        }

    }
}
