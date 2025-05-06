using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Translator
{
    public enum LexemeType
    {
        Id,
        IntNumber,
        FloatNumber,
        Int,
        Float,
        ArrayInt,
        ArrayFloat,
        If,
        Else,
        While,
        Read,
        Write,
        LeftBrace,
        RightBrace,
        LeftSquareBracket,
        RightSquareBracket,
        LeftRoundBracket,
        RightRoundBracket,
        Plus,
        Minus,
        Multiply,
        Divide,
        Semicolon,
        Dot,
        Less,
        Assign,
        More,
        Equal,
        LessOrEqual,
        MoreOrEqual,
        NotEqual,
        Finish,
        Error
    }

    public class Lexeme
    {
        // тип лексемы
        public LexemeType lexeme_type;
        
        // текстовое значение 
        public string Value;

        // целочисленное значение
        public int N;

        // вещественное значение
        public double F;

        // номер строки 
        public int Line;

        // позиция
        public int Position;

        public string State;
        public int Program;
    }

    class LexemeAnalyzer
    {
        private enum State
        {
            S,     // S - начальное состояние
            A,     // A - идентификаторы/ключевые слова
            B,     // B - целые числа
            C,     // C - точка в вещественном числе
            D,     // D - дробная часть
            F,     // F - успешное завершение
            O      // O - ошибка
        }

        private LexemeType GetKeywordType(string ident)
        {
            switch (ident)
            {
                case "int": return LexemeType.Int;
                case "float": return LexemeType.Float;
                case "arrayInt": return LexemeType.ArrayInt;
                case "arrayFloat": return LexemeType.ArrayFloat;
                case "if": return LexemeType.If;
                case "else": return LexemeType.Else;
                case "while": return LexemeType.While;
                case "read": return LexemeType.Read;
                case "write": return LexemeType.Write;
                default: return LexemeType.Id;
            };
        }

        // Типы символов
        private enum CharType
        {
            Letter,
            Digit,
            ComparisonOp,
            ArithmOp,
            OpenBracket,
            CloseBracket, 
            Semicolon,    // ;
            NewLine,      // \n
            At,           // @
            Dot,          // .
            Assign,       // =
            OpenBrace,    // {
            CloseBrace,   // }
            Other
        }

        private CharType GetCharType(char c)
        {
            if (char.IsLetter(c)) return CharType.Letter;
            if (char.IsDigit(c)) return CharType.Digit;

            switch (c)
            {
                case ';': return CharType.Semicolon;
                case '<':
                case '>':
                case '#':
                case '%':
                case '$':
                case '~': return CharType.ComparisonOp;
                case '=': return CharType.Assign;
                case '/':
                case '*':
                case '+':
                case '-': return CharType.ArithmOp;
                case '[': 
                case '(': return CharType.OpenBracket;
                case ')': 
                case ']': return CharType.CloseBracket;
                case '{': return CharType.OpenBrace;
                case '}': return CharType.CloseBrace;
                case '\n': return CharType.NewLine;
                case '@': return CharType.At;
                case '.': return CharType.Dot;
                default: return CharType.Other;
            };
        }

        private string programText;
        private int currentIndex;
        private State currentState;
        private int n = 0;
        private double x = 0, d = 0;

        // список разпознанных лексем
        List<Lexeme> data = new List<Lexeme>();
        public List<Lexeme> GetData()
        {
            return data;
        }

        public LexemeAnalyzer(string prText)
        {
            programText = prText;
            currentIndex = 0;
            currentState = State.S;
        }

        public void Run()
        {
            Lexeme currentLexeme = new Lexeme();
            int currentLine = 1;
            int currentPos = 1;

            while (currentIndex < programText.Length)
            {
                // Пропускаем пробелы и табы
                while (currentIndex < programText.Length && (programText[currentIndex] == ' ' || programText[currentIndex] == '\t' || programText[currentIndex] == '\n'))
                {
                    if (programText[currentIndex] == ' ')
                        currentPos++;
                    else if (programText[currentIndex] == '\n')
                    {
                        currentLine++; 
                        currentPos = 1;
                    }
                    else
                        currentPos += 4;
                    currentIndex++;
                    
                }

                if (currentIndex >= programText.Length) break;

                currentLexeme = NextLexeme();
                currentLexeme.Line = currentLine;
                currentLexeme.Position = currentPos;

                if (currentLexeme.lexeme_type == LexemeType.Error)
                {
                    string msg = $"Ошибка анализатора: строка = {currentLine}, позиция = {currentPos - 1}";
                    throw new Exception(msg);
                }

                if (currentLexeme.Value != null)
                    currentPos += currentLexeme.Value.Length;
                else if (currentLexeme.lexeme_type == LexemeType.IntNumber)
                    currentPos += currentLexeme.N.ToString().Length;
                else if (currentLexeme.lexeme_type == LexemeType.FloatNumber)
                    currentPos += currentLexeme.F.ToString().Length;
                else
                    currentPos += 1;

                currentLexeme.State = currentState.ToString();
                data.Add(currentLexeme);
                currentIndex++;
                currentPos++;
            }

            // Добавляем маркер конца
            data.Add(new Lexeme
            {
                lexeme_type = LexemeType.Finish,
                Line = currentLine,
                Position = currentPos
            });
        }

        private Lexeme NextLexeme()
        {
            // по умолчанию ошибка
            Lexeme result = new Lexeme();
            result.lexeme_type = LexemeType.Error;
            StringBuilder value = new StringBuilder();
            currentState = State.S;

            char current_char = programText[currentIndex];
           
            // Добавляем первый символ
            if (currentIndex < programText.Length)
            {
                 if (data.Count == 0)
                 {
                     value.Append(current_char);
                     currentIndex++;
                 }
            }
            
            while (currentIndex < programText.Length && currentState != State.F && currentState != State.O)
            {
                char currentChar = programText[currentIndex];

                if (currentChar == ' ') break;

                CharType charType = GetCharType(currentChar);

                // Получаем переход из таблицы
                var transition = GetTransition(currentState, charType, currentChar);
                currentState = transition.NewState;

                // Выполняем действие
                if (transition.Action != -1)
                {
                    ExecuteAction(transition.Action, result, value.ToString(), currentChar);
                }

                if (currentState != State.F && currentState != State.O)
                {
                    value.Append(currentChar);
                    currentIndex++;
                    result.Position++;
                }

                result.Program = transition.Action;
            }

            // После выхода из цикла проверяем конечное состояние
             switch (currentState)
            {
                case State.A:
                    result.lexeme_type = GetKeywordType(value.ToString());
                    result.Value = value.ToString();
                    break;
                case State.B:
                    result.lexeme_type = LexemeType.IntNumber;
                    result.N = int.Parse(value.ToString());
                    break;
                case State.D:
                    result.lexeme_type = LexemeType.FloatNumber;
                    result.F = double.Parse(value.ToString());
                    break;
                case State.O:
                    result.lexeme_type = LexemeType.Error;
                    result.Value = "Нераспознанная лексема: " + value.ToString();
                    break;
            }

            return result;
        }
        
        private (State NewState, int Action) GetTransition(State currentState, CharType charType, char currentChar)
        {
            // Таблица переходов
            switch (currentState)
            {
                case State.S:
                    switch (charType)
                    {
                        case CharType.Letter: return (State.A, 0);
                        case CharType.Digit: return (State.B, 2);
                        case CharType.ComparisonOp: return (State.F, 4);
                        case CharType.ArithmOp: return (State.F, 6);
                        case CharType.OpenBracket: return (State.F, 7);
                        case CharType.CloseBracket: return (State.F, 8);
                        case CharType.Semicolon: return (State.F, 9);
                        case CharType.NewLine: return (State.S, 20);
                        case CharType.At: return (State.F, 10);
                        case CharType.Dot: return (State.F, 13);
                        case CharType.Assign: return (State.F, 5);
                        case CharType.OpenBrace: return (State.F, 11);
                        case CharType.CloseBrace: return (State.F, 12);
                        default: return (State.O, 16);
                    }

                case State.A:
                    switch (charType)
                    {
                        case CharType.Letter:
                        case CharType.Digit: return (State.A, 1);
                        case CharType.NewLine: return (State.A, 21); 
                        case CharType.OpenBrace: return (State.O, 19);
                        case CharType.Other: return (State.O, 16);
                        default: return (State.F, 14);
                    }

                case State.B:
                    switch (charType)
                    {
                        case CharType.Letter: return (State.O, 17);
                        case CharType.Digit: return (State.B, 3);
                        case CharType.ComparisonOp: 
                        case CharType.ArithmOp: 
                        case CharType.CloseBracket: 
                        case CharType.Semicolon: 
                        case CharType.At: return (State.F, 15);
                        case CharType.NewLine: return (State.B, 18);
                        case CharType.Dot: return (State.C, 22);
                        case CharType.Other: return (State.O, 16);
                        default: return (State.O, 19);
                    }

                case State.C:
                    switch (charType)
                    {
                        case CharType.Digit: return (State.D, 22);
                        default: return (State.O, 19);
                    }

                case State.D:
                    switch (charType)
                    {
                        case CharType.Digit: return (State.D, 23);
                        case CharType.ComparisonOp:
                        case CharType.ArithmOp:
                        case CharType.CloseBracket:
                        case CharType.Semicolon:
                        case CharType.At: return (State.F, 15);
                        case CharType.NewLine: return (State.D, 18);
                        case CharType.Other: return (State.O, 16);
                        default: return (State.O, 19);
                    }

                default:
                    return (State.O, 19);
            }
        }

        private void ExecuteAction(int action, Lexeme lexeme, string value, char currentChar)
        {
            // Семантические программы
            switch (action)
            {
                case 0: // Начало идентификатора
                    lexeme.lexeme_type = LexemeType.Id;
                    break;
                case 1: // Продолжение идентификатора
                    lexeme.Value += currentChar;
                    break;
                case 2: // Начало числа
                    n = currentChar - '0';
                    lexeme.lexeme_type = LexemeType.IntNumber;
                    break;
                case 3: // Продолжение числа
                    n = n * 10 + currentChar - '0';
                    lexeme.N = n;
                    break;
                case 4: // Оператор сравнения
                    switch (currentChar)
                    {
                        case '<': lexeme.lexeme_type = LexemeType.Less; break;
                        case '>': lexeme.lexeme_type = LexemeType.More; break;
                        case '#': lexeme.lexeme_type = LexemeType.Equal; break;
                        case '~': lexeme.lexeme_type = LexemeType.NotEqual; break;
                        case '$': lexeme.lexeme_type = LexemeType.MoreOrEqual; break;
                        case '%': lexeme.lexeme_type = LexemeType.LessOrEqual; break;
                    }
                    break;
                case 5: // Присваивание
                    lexeme.lexeme_type = LexemeType.Assign;
                    break;
                case 6: // Ариф операция
                    switch (currentChar)
                    {
                        case '/': lexeme.lexeme_type = LexemeType.Divide; break;
                        case '*': lexeme.lexeme_type = LexemeType.Multiply; break;
                        case '+': lexeme.lexeme_type = LexemeType.Plus; break;
                        case '-': lexeme.lexeme_type = LexemeType.Minus; break;
                    }
                    break;
                case 7: // Открывающая скобка
                    switch (currentChar)
                    {
                        case '(': lexeme.lexeme_type = LexemeType.LeftRoundBracket; break;
                        case '[': lexeme.lexeme_type = LexemeType.LeftSquareBracket; break;
                    }
                    break;
                case 8: // Закрывающая скобка
                    switch (currentChar)
                    {
                        case ')': lexeme.lexeme_type = LexemeType.RightRoundBracket; break;
                        case ']': lexeme.lexeme_type = LexemeType.RightSquareBracket; break;
                    }
                    break;
                case 9: // ;
                    lexeme.lexeme_type = LexemeType.Semicolon;
                    break;
                case 10: // @
                    lexeme.lexeme_type = LexemeType.Finish;
                    break;
                case 11: // {
                    lexeme.lexeme_type = LexemeType.LeftBrace;
                    break;
                case 12: // }
                    lexeme.lexeme_type = LexemeType.RightBrace;
                    break;
                case 13: // .
                    lexeme.lexeme_type = LexemeType.Dot;
                    break;

                case 14: // Поиск идентификатора в служ
                    lexeme.lexeme_type = GetKeywordType(value);
                    currentIndex--;
                    break;

                case 15: // Распознано число
                    currentIndex--;
                    break;

                case 16: // Распознан символ, не относящийся к языку
                    lexeme.Value = "Недопустимый символ: " + currentChar;
                    lexeme.lexeme_type = LexemeType.Error;
                    break;

                case 17: // Ошибка в лексеме
                    lexeme.Value = "Некорректная лексема: " + lexeme.Value;
                    lexeme.lexeme_type = LexemeType.Error;
                    break;

                case 18: // Распознано число + переход на новую строку
                    lexeme.Line++;
                    break;

                case 19: // Ошибка во входной строке
                    lexeme.Value = "Ошибка в строке";
                    lexeme.lexeme_type = LexemeType.Error;
                    break;

                case 20: // Перевод строки
                    lexeme.Line++;
                    lexeme.Position = 1;
                    break;

                case 21: // Распознано служ слово \n 
                    lexeme.Value += currentChar;
                    lexeme.lexeme_type = GetKeywordType(lexeme.Value);
                    lexeme.Line++;
                    lexeme.Position = 1;
                    break;

                case 22: // Подготовка к дробной части
                    d = 1;
                    x = n;
                    lexeme.F = x;
                    lexeme.lexeme_type = LexemeType.FloatNumber;
                    break;

                case 23: // Обработка дробной части
                    d *= 0.1;
                    x += (currentChar - '0') * d;
                    lexeme.F = x;
                    break;

                default:
                    lexeme.lexeme_type = LexemeType.Error;
                    break;
            }
        }
    }
}
