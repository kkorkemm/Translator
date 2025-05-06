using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Тестовый код для анализа int x=3;@ arrayInt arr[10];@ if(x>2.5){write(x);}else{read(x);}@ while(x<10) {x=x+1;}
            string testCode = @" int x = 1; float a = 1.34;";

            Console.WriteLine("Исходный код:");
            Console.WriteLine(testCode);
            Console.WriteLine("\nРезультат лексического анализа:");

            try
            {
                LexemeAnalyzer lexer = new LexemeAnalyzer(testCode);
                lexer.Run();

                Console.WriteLine($"Позиция \t|Состояние \t|Программа\t|Тип лексемы \t|");
                foreach (var lexeme in lexer.GetData())
                {
                    Console.WriteLine($"{lexeme.Position}\t\t {lexeme.State}\t\t {lexeme.Program}\t\t {lexeme.lexeme_type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.ReadKey();
        
        }
    }
}
