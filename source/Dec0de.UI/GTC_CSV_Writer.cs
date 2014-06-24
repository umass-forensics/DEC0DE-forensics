using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Dec0de.Bll.Viterbi;
using Dec0de.UI.PostProcess;
using Dec0de.Bll;
using System.Runtime.Serialization.Formatters.Binary;

namespace Dec0de.UI
{
    public class GTC_CSV_Writer
    {
        private List<MetaResult> _metaResults;
        private PostProcessor _postProcess;
        private string _filePath;
        private Dictionary<int, List<ViterbiField>> _fields_in_blocks;        
        private Dictionary<int, List<ViterbiField>> _fields_of_interest;

        public GTC_CSV_Writer(List<MetaResult> metaResults, PostProcessor postProcess, string filePath)
        {
            _metaResults = metaResults;
            _postProcess = postProcess;
            _filePath = filePath;
            _fields_in_blocks = new Dictionary<int, List<ViterbiField>>();
            _fields_of_interest = new Dictionary<int, List<ViterbiField>>();
            Get_Fields_in_Blocks();
            Get_Fields_Of_Interest();            
        }

        private void Get_Fields_in_Blocks()
        {
            int block_ctr = 0;
            List<int> binaryLarge_indices = new List<int>();

            //getting indices of all Binary Large fields
            for (int i = 0; i < _metaResults.Count; i++)
            {
                if (_metaResults[i].Name.ToString() == "BinaryLarge")
                {
                    binaryLarge_indices.Add(i);
                }
            }

            // getting fields lying between Binary Large Fields
            for (int i = 0; i < binaryLarge_indices.Count - 1; i++)
            {
                block_ctr++;
                int beg_block_field_index = binaryLarge_indices[i] + 1;
                int end_block_field_index = binaryLarge_indices[i + 1] - 1;
                List<ViterbiField> fields_for_this_block = new List<ViterbiField>();

                for (int j = beg_block_field_index; j <= end_block_field_index; j++)
                {
                    fields_for_this_block.Add(_metaResults[j].Field);
                }
                _fields_in_blocks.Add(block_ctr, fields_for_this_block);
            }

            int beg_index = 0;

            // getting the fields in the block that is after the last binary large field
            if (binaryLarge_indices.Count != 0)
                beg_index = binaryLarge_indices[binaryLarge_indices.Count - 1] + 1;
            
            List<ViterbiField> fields_for_last_block = new List<ViterbiField>();                
            for (int i = beg_index; i < _metaResults.Count; i++)
            {
                fields_for_last_block.Add(_metaResults[i].Field);
            }
            block_ctr++;
            _fields_in_blocks.Add(block_ctr, fields_for_last_block);
        }

        private void Get_Fields_Of_Interest()
        {
            for (int i = 0; i < _postProcess.addressBookFields.Count; i++)
            {
                long Offset = _postProcess.addressBookFields[i].MetaData.Offset;
                KeyValuePair<int, List<ViterbiField>> pair = Get_Block_Fields_Given_Offset(Offset);
                if (!_fields_of_interest.ContainsKey(pair.Key) && pair.Value != null)
                {
                    _fields_of_interest.Add(pair.Key, pair.Value);
                }
            }

            for (int i = 0; i < _postProcess.callLogFields.Count; i++)
            {
                long Offset = _postProcess.callLogFields[i].MetaData.Offset;
                KeyValuePair<int, List<ViterbiField>> pair = Get_Block_Fields_Given_Offset(Offset);
                if (!_fields_of_interest.ContainsKey(pair.Key) && pair.Value != null)
                {
                    _fields_of_interest.Add(pair.Key, pair.Value);
                }
            }

            for (int i = 0; i < _postProcess.smsFields.Count; i++)
            {
                long Offset = _postProcess.smsFields[i].MetaData.Offset;
                KeyValuePair<int, List<ViterbiField>> pair = Get_Block_Fields_Given_Offset(Offset);
                if (!_fields_of_interest.ContainsKey(pair.Key) && pair.Value != null)
                {
                    _fields_of_interest.Add(pair.Key, pair.Value);
                }
            }
        }

        // Given the offset of a valid record (from postProcess), it returns all the fields in its block
        private KeyValuePair<int, List<ViterbiField>> Get_Block_Fields_Given_Offset(long Offset)
        {
            KeyValuePair<int, List<ViterbiField>> p = new KeyValuePair<int, List<ViterbiField>>();
            foreach (KeyValuePair<int, List<ViterbiField>> pair in _fields_in_blocks)
            {
                long block_beg = pair.Value[0].OffsetFile;
                long block_end = pair.Value[pair.Value.Count - 1].OffsetFile + pair.Value[pair.Value.Count - 1].Length - 1;

                if (Offset >= block_beg && Offset <= block_end)
                {
                    p = pair;
                    break;
                }
            }
            return p;
        }

        public void Write_CSV()
        {
            TextWriter tw = null;
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                string fn = Path.GetFileNameWithoutExtension(_filePath);
                tw =
                    new StreamWriter(Path.Combine(dir,
                                                    String.Format("{0}_{1}.csv", fn,
                                                                DateTime.Now.ToString("yyyyMMdd_HHmm"))));                

                foreach (KeyValuePair<int, List<ViterbiField>> pair in _fields_in_blocks /*_fields_of_interest*/)
                {
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        ViterbiField field = pair.Value[i];
                        if (field.MachineName.ToString() == "Start")
                        {
                            /// happens with the Binary field.
                            field.FieldString = "???";
                        }
                        tw.WriteLine(pair.Key + "\t" + field.MachineName.ToString() + "\t" + field.OffsetFile.ToString() + 
                            "\t" + field.Length.ToString() + "\t" + field.FieldString.ToString() + "\t" + "NA");
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (tw != null) tw.Close();
            }
        }

        public void Write_Field_Paths(List<FieldPaths> all_paths)
        {
            string outputfile = _filePath + "_paths.vtf";

            using (Stream outstream = File.Create(outputfile))
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(outstream, all_paths);
            }            
        }
    }
}
