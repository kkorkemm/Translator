using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Translator;
using static System.Net.Mime.MediaTypeNames;

namespace Translator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\nРезультат лексического анализа:");

            Console.WriteLine($"Позиция \t|Состояние \t|Программа\t|Тип лексемы \t|");
            string text = "";

            LexemeAnalyzer lexer = new LexemeAnalyzer(text);
            using (StreamReader fs = new StreamReader(@"testA.txt"))
            {
                while (text != null)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    text = temp;

                    try
                    {
                        lexer = new LexemeAnalyzer(text);
                        lexer.Run();

                        foreach (var lexeme in lexer.GetData())
                        {
                            lexeme.Line = LexemeAnalyzer.currentLine;
                            Console.WriteLine($"{lexeme.Position}\t\t {lexeme.State}\t\t {lexeme.Program}\t\t {lexeme.lexeme_type}");
                        }

                        OpsGenerator opsGenerator = new OpsGenerator(lexer.GetData());
                        opsGenerator.Run();

                        foreach (var op in opsGenerator.get_data().ops)
                        {
                            Console.WriteLine(op.type);

                            if (op.type == OpsItemType.Operation)
                                Console.WriteLine(op.operation);

                            if (op.type == OpsItemType.VariableName || op.type == OpsItemType.Metka)
                                Console.WriteLine(op.var_name);
                            else if (op.type == OpsItemType.IntNumber)
                                Console.WriteLine(op.int_num);
                            else if (op.type == OpsItemType.FloatNumber)
                                Console.WriteLine(op.float_num);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}");
                    }
                    LexemeAnalyzer.currentLine++;
                }
            }

            

            Console.ReadKey();        
        }
    }
}
