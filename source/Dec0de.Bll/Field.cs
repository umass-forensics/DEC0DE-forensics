using System.Collections.Generic;

namespace Dec0de.Bll
{
    public class Field
    {
        public string Type { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public override string ToString()
        {
            return string.Format(" [({0},{1}), \'{2}\'] ", Start, Length, Type);
        }

        public static Field[] CompressBinaryFields(Field[] input)
        {
            List<Field> tmp = new List<Field>();

            for (int i = 0; i < input.Length-1; i++)
            {
                bool leftIsBinary = input[i].Type == "Binary" || input[i].Type == "Byte" || input[i].Type == "Null";

                if (leftIsBinary)
                {
                    int length = input[i].Length;
                    int start = input[i].Start;


                    for (int j = i + 1; j < input.Length; j++)
                    {
                        bool rightIsBinary = input[j].Type == "Binary" || input[j].Type == "Byte" || input[j].Type == "Null";

                        if (rightIsBinary)
                        {
                            length += input[j].Length;
                        }
                        
                        if (!rightIsBinary || j == input.Length-1)
                        {
                            Field newField = new Field()
                            {
                                Type = "Binary",
                                Length = length,
                                Start = start
                            };

                            tmp.Add(newField);

                            i = j - 1;

                            break;
                        }
                    }
                }
                else
                {
                    tmp.Add(input[i]);
                }
            }

            return tmp.ToArray();

        }
    }
}
