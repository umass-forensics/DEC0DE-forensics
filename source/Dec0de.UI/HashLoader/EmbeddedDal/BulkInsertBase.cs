/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Dec0de.UI.HashLoader.EmbeddedDal
{
    public class HashInfo
    {
        public int HashRunId;
        public long BlockIndex;
        public string Hash;
    }

    public class BulkInsert_PacketInfo : BulkInsertBase
    {
        public BulkInsert_PacketInfo() :
            base("tbl_hash", 10000) { }

        protected override void InitializeStructures()
        {
            this._dataTable.Columns.Add("hashRunId", typeof(Int32));
            this._dataTable.Columns.Add("blockIndex", typeof(Int64));
            this._dataTable.Columns.Add("hash", typeof(string));
        }

        protected override void PopulateDataTable(object infoClass)
        {
            HashInfo info = (HashInfo)infoClass;

            DataRow row;
            // populate the values
            // using your custom logic
            row = _dataTable.NewRow();

            row[0] = info.HashRunId;
            row[1] = info.BlockIndex;
            row[2] = info.Hash;

            // add it to the base for final addition to the DB
            _dataTable.Rows.Add(row);
            _recordCount++;
        }

    }

    public abstract class BulkInsertBase
    {
        private List<HashInfo> _hashes;

        protected string _tableName;
        protected DataTable _dataTable = new DataTable();
        protected int _recordCount;
        protected int _commitBatchSize;

        protected BulkInsertBase(string tableName, int commitBatchSize)
        {
            this._tableName = tableName;
            this._dataTable = new DataTable(tableName);
            this._recordCount = 0;
            this._commitBatchSize = commitBatchSize;

            // add columns to this data table
            InitializeStructures();
        }

        protected abstract void InitializeStructures();

        public static BulkInsertBase Load(List<HashInfo> hashes)
        {
            // create a new object to return
            BulkInsertBase bulker = new BulkInsert_PacketInfo();

            bulker._hashes = hashes;

            return bulker;
        }

        public void Flush()
        {
            // transfer data to the datatable
            for (int i = 0; i < _hashes.Count; i++) {
                PopulateDataTable(_hashes[i]);

                if (_recordCount >= _commitBatchSize)
                    WriteToDatabase();
            }
            // write remaining records to the DB
            if (_recordCount > 0)
                WriteToDatabase();
        }

        protected abstract void PopulateDataTable(object recordClass);

        private void WriteToDatabase()
        {
            // get your connection string
            string connString = @"Data Source=BLPHONEPC\SQLEXPRESS;Initial Catalog=PhoneDecode;Integrated Security=True";

            // connect to SQL
            using (SqlConnection connection = new SqlConnection(connString)) {
                // make sure to enable triggers
                // more on triggers in next post
                SqlBulkCopy bulkCopy =
                    new SqlBulkCopy
                    (
                    connection,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.UseInternalTransaction |
                    SqlBulkCopyOptions.CheckConstraints,
                    null
                    );

                // set the destination table name
                bulkCopy.DestinationTableName = this._tableName;
                connection.Open();

                // write the data in the "dataTable"
                bulkCopy.WriteToServer(_dataTable);
                connection.Close();
            }
            // reset
            this._dataTable.Clear();
            this._recordCount = 0;
        }
    }
}
