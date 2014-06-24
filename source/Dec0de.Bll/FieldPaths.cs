using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dec0de.Bll
{
    [Serializable()]
    public class FieldPaths
    {        
        /// <summary>
        /// beginning offset of the first field state machine
        /// </summary>
        public long _path_beg_offset;
        /// <summary>
        /// beginning offset of the last field state machine
        /// </summary>
        public long _path_end_offset;
        /// <summary>
        /// List of all the field state machines in the path
        /// </summary>
        public List<string> _fields_in_path;

        public string _actual_path;

        public FieldPaths()
        {  
            _fields_in_path = new List<string>();
            _actual_path = string.Empty;
        }

        public FieldPaths(FieldPaths fp_obj)
        {
            _actual_path = fp_obj._actual_path;
            _fields_in_path = new List<string>(fp_obj._fields_in_path);
            _path_beg_offset = fp_obj._path_beg_offset;
            _path_end_offset = fp_obj._path_end_offset;
        }

        public void find_actual_path()
        {            
            for (int i = 0; i < _fields_in_path.Count; i++)
            {
                _actual_path += _fields_in_path[i];
                if (i != _fields_in_path.Count - 1)
                {
                    _actual_path += ",";
                }
            }
        }        
    }
}
