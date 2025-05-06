using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Translator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Тестовый код для анализа int x=3;@ arrayInt arr[10];@ if(x>2.5){write(x);}else{read(x);}@ while(x<10) {x=x+1;}
           //tring testCode = @"if(x $ 2) else {x}; write(x); ";

            //Console.WriteLine("Исходный код:");
            //Console.WriteLine(testCode);
            Console.WriteLine("\nРезультат лексического анализа:");

            Console.WriteLine($"Позиция \t|Состояние \t|Программа\t|Тип лексемы \t|");
            string text = "";
            using (StreamReader fs = new StreamReader(@"kal.txt"))
            {
                while (text != null)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    text = temp;

                    try
                    {
                        LexemeAnalyzer lexer = new LexemeAnalyzer(text);
                        lexer.Run();

                        foreach (var lexeme in lexer.GetData())
                        {
                            Console.WriteLine($"{lexeme.Position}\t\t {lexeme.State}\t\t {lexeme.Program}\t\t {lexeme.lexeme_type}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
            }

           

            Console.ReadKey();
        
        }
    }
}
