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
            string text = "";
            string temp = "";

            Console.Write("Выберите тест (A, B, C, D): ");
            string test = Console.ReadLine();

            using (StreamReader fs = new StreamReader($@"test{test}.txt"))
            {
                while (temp != null)
                {
                    temp = fs.ReadLine();
                    text += temp + '\n';
                }
            }
           
            LexemeAnalyzer lexer = new LexemeAnalyzer(text);
            lexer.Run();

            Console.WriteLine("\nРезультат лексического анализа:");
            Console.WriteLine($"Позиция \t|Строка \t|Состояние \t|Программа\t|Тип лексемы \t|");
            foreach (var lexeme in lexer.GetData())
            {
                Console.WriteLine($"{lexeme.Position}\t\t{lexeme.Line}\t\t {lexeme.State}\t\t {lexeme.Program}\t\t {lexeme.lexeme_type}");
            }

            OpsGenerator opsGenerator = new OpsGenerator(lexer.GetData());
            opsGenerator.Run();

            Console.WriteLine("\n");
            Console.WriteLine($"Тип\t\t\t|Строка\t\t|Номер\t\t|ОПС \t\t\t|");
            int i = 0;
            foreach (var op in opsGenerator.get_data().ops)
            {
                string ops = "";
                if (op.type == OpsItemType.Operation)
                    ops = op.operation.ToString();

                if (op.type == OpsItemType.VariableName)
                    ops = op.var_name;
                else if (op.type == OpsItemType.IntNumber)
                    ops = op.int_num.ToString();
                else if (op.type == OpsItemType.FloatNumber)
                    ops = op.float_num.ToString();
                else if (op.type == OpsItemType.MarkerName)
                    ops = "m" + op.pos;
                else if (op.type == OpsItemType.ArrayInt)
                    ops = op.var_name;
                else if (op.type == OpsItemType.ArrayFloat)
                    ops = op.var_name;

                Console.WriteLine($"{op.type}\t\t {op.line}\t\t {i}\t\t {ops}\t\t");
                i++;
            }
            Console.WriteLine("\n");

            OpsInterpretator opsInterpreter = new OpsInterpretator(opsGenerator.get_data());
            opsInterpreter.Run();

            Console.ReadKey();        
        }
    }
}
