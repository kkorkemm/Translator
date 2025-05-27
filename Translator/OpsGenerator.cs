using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Translator
{
    /// <summary>
    /// Тип данных в ОПС
    /// </summary>
    public enum OpsItemType
    {
        VariableName,
        IntNumber,
        FloatNumber,
        Operation,
        MarkerName,
        ArrayInt,
        ArrayFloat,
        Error
    }

    /// <summary>
    /// Операции ОПС
    /// </summary>
    public enum OpsItemOperation
    {
        Read,
        Write,
        Plus,
        Minus,
        Miltiply,
        Divide,
        Less,
        Assign,
        More,
        Equal,
        LessOrEqual,
        MoreOrEqual,
        NotEqual,
        Jump,
        JumpIfFalse,
        Index,
        Error
    }

    /// <summary>
    /// Компонент ОПС
    /// </summary>
    public class OpsItem
    {
        public OpsItemType type = OpsItemType.Error;
        public OpsItemOperation operation = OpsItemOperation.Error;
        public string var_name;
        public int int_num = 0;
        public double float_num = 0;
        public int line;
        public int pos;
        public MarkerName metka;

        public int[] arrayInt;
        public double[] arrayFloat;

        public OpsItem(string name, Lexeme l)
        {
            type = OpsItemType.VariableName;
            var_name = name;
            line = l.Line;
            pos = l.Position;
        }

        public OpsItem(OpsItemOperation op, Lexeme l)
        {
            type = OpsItemType.Operation;
            operation = op;
            line = l.Line;
            pos = l.Position;
        }


        public OpsItem(int number, Lexeme l)
        {
            type = OpsItemType.IntNumber;
            int_num = number;
            line = l.Line;
            pos = l.Position;
        }

        public OpsItem(double number, Lexeme l)
        {
            type = OpsItemType.FloatNumber;
            float_num = number;
            line = l.Line;
            pos = l.Position;
        }

        public OpsItem(int number, string name, bool isArray)
        {
            type = OpsItemType.ArrayInt;
            int_num = number;
            var_name = name;
        }

        public OpsItem(double number, string name, bool isArray)
        {
            type = OpsItemType.ArrayFloat;
            float_num = number;
            var_name = name;
        }

        public OpsItem(int id, int idx, string name, bool isArray)
        {
            type = OpsItemType.ArrayFloat;
            int_num = idx;
            var_name = name;
        }

        public OpsItem(int number, int l, int p)
        {
            type = OpsItemType.IntNumber;
            int_num = number;
            line = l;
            pos = p;
        }

        public OpsItem(double number, int l, int p)
        {
            type = OpsItemType.FloatNumber;
            float_num = number;
            line = l;
            pos = p;
        }

        public OpsItem(MarkerName metka, int l)
        {
            this.metka = metka;
            type = OpsItemType.MarkerName;
            line = l;
            var_name = metka.name;
        }

        public OpsItem(MarkerName metka, int l, int p)
        {
            this.metka = metka;
            type = OpsItemType.MarkerName;
            line = l;
            var_name = metka.name;
            pos = p;
        }
    }

    public class MarkerName
    {
        public string name;

        public MarkerName(string metkaname)
        {
            name = metkaname;
        }
    }

    /// <summary>
    /// Сгенерированный ОПС
    /// </summary>
    public struct InterpretData
    {
        public List<OpsItem> ops;
    }

    public class OpsGenerator
    {
        /// <summary>
        /// Нетерминалы (грамматика)
        /// </summary>
        private enum State
        {
            S, //  intQS | float QS | arrayIntQS | arrayFloatQS | ... (Q)
            Q, //  aH = E;Q | read(aH);Q | write(E);Q | if (C) {AQ}KZQ | while (C) {AQ}Q | λ
            A, //  aH = E; | read(aH); | write(E); | if (C) {AQ}KZ | while (C) {AQ}
            I, //  aM
            M, //  ,aM | ;
            P, //  a[i]N
            N, //  ,a[i]N | ;
            H, //  [E] | λ
            C, //  (E)VUL | aHVUL | iVUL | fVUL
            L, //  <EZ | >EZ | == EZ | ≤EZ | ≥EZ | !=EZ
            K, //  else { SQ } | λ
            E, //  (E)VU | aHVU | iVU | fVU
            U, //  + TU | -TU | λ
            T, //  (E)V | aHV | iV | fV
            V, //  *FV | /FV | λ
            F, //  (E) | aH | i | f
            Z, //  λ
            Error 
        }

        private enum GeneratorTask
        {
            Empty,
            VariableId,
            IntNumber,
            FloatNumber,
            ArrayInt,
            ArrayFloat,
            Read,
            Write,
            Plus,
            Minus,
            Multiply,
            Divide,
            Less,
            Assign,
            More,
            Equal,
            LessOrEqual,
            MoreOrEqual,
            NotEqual,
            Index,
            Task1,
            Task2,
            Task3,
            Task4,
            Task5
        }

        private struct MagazineItem
        {
            public bool is_terminal;
            public LexemeType lexeme;
            public State state;

            public MagazineItem(LexemeType l)
            {
                is_terminal = true;
                lexeme = l;
                state = State.Error;
            }

            public MagazineItem(State s)
            {
                is_terminal = false;
                lexeme = LexemeType.Error;
                state = s;
            }
        }

        private GeneratorTask current_task;
        private Lexeme current_lexeme;
        private State current_state;
        private Stack<MagazineItem> Magazine = new Stack<MagazineItem>();
        private Stack<GeneratorTask> Generator = new Stack<GeneratorTask>();
        private Stack<MarkerName> Marks = new Stack<MarkerName>();


        private List<Lexeme> input_data;
        /// <summary>
        /// Получаем данные от лексического анализатора
        /// </summary>
        public OpsGenerator(List<Lexeme> lexemes)
        {
            input_data = lexemes;
        }

        private InterpretData data;
        public InterpretData get_data()
        {
            return data;
        }

        public void Run()
        {
            data.ops = new List<OpsItem>();

            Magazine.Push(new MagazineItem(State.S));
            Generator.Push(GeneratorTask.Empty);

            int current_lexeme_idx = 0;
            current_lexeme = input_data[current_lexeme_idx];

            while (Generator.Count != 0 && Magazine.Count != 0)
            {
                MagazineItem current_magazine_item = Magazine.Peek(); 
                Magazine.Pop();
                current_state = current_magazine_item.state;
                current_task = Generator.Peek(); 
                Generator.Pop();

                run_task();

                if (current_magazine_item.is_terminal)
                {

                    if (current_lexeme.lexeme_type == LexemeType.Finish)
                    {
                        string msg = "Лексемы прочитаны, но магаз не пустой.  Строка: "
                            + Convert.ToString(current_lexeme.Line) + ", позиция: " + Convert.ToString(current_lexeme.Position);
                        throw new Exception(msg);
                    }

                    if (current_magazine_item.lexeme == current_lexeme.lexeme_type)
                    {
                        current_lexeme_idx++;
                        current_lexeme = input_data[current_lexeme_idx];
                    }
                    else
                    {
                        string msg = "Некорректная лексема. Строка:"
                            + Convert.ToString(current_lexeme.Line) + ", позиция:" + Convert.ToString(current_lexeme.Position);
                        throw new Exception(msg);
                    }
                }
                else
                {
                    run_rule();
                }
            }

            if (current_lexeme.lexeme_type != LexemeType.Finish)
            {
                string msg = "Остались нераспознанные лексемы. Строка: "
                    + Convert.ToString(current_lexeme.Line) + ", позиция: " + Convert.ToString(current_lexeme.Position);
                throw new Exception(msg);
            }
        }

        private void run_rule()
        {
            string msg;
            switch (current_state)
            {
                case State.S:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Int:
                        {
                            Magazine.Push(new MagazineItem(State.S));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Assign));
                            //Magazine.Push(new MagazineItem(State.I));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.Int));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Assign);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Float:
                        {
                            Magazine.Push(new MagazineItem(State.S));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Assign));
                            //Magazine.Push(new MagazineItem(State.I));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.Float));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Assign);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);

                            break;
                        }
                        case LexemeType.ArrayInt:
                        {
                            Magazine.Push(new MagazineItem(State.S));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));
                            Magazine.Push(new MagazineItem(LexemeType.LeftSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.ArrayInt));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Index);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.IntNumber);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.ArrayFloat:
                        {
                                    ///
                            Magazine.Push(new MagazineItem(State.S));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightSquareBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.ArrayFloat));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Index);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Assign));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Assign);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.Read:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Read));

                            Generator.Push(GeneratorTask.Read);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Write:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Write));

                            Generator.Push(GeneratorTask.Write);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.If:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.K));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.If));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task3);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.While:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.While));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task5);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task4);
                            break;
                        }
                            case LexemeType.Finish:
                                break;
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.I:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.M));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                                    Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.M:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Dot:
                        {
                            Magazine.Push(new MagazineItem(State.M));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.Dot));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Semicolon:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));

                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.P:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.N));
                            Magazine.Push(new MagazineItem(LexemeType.RightSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));
                            Magazine.Push(new MagazineItem(LexemeType.LeftSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Index);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.N:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Dot:
                        {
                            Magazine.Push(new MagazineItem(State.N));
                            Magazine.Push(new MagazineItem(LexemeType.RightSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));
                            Magazine.Push(new MagazineItem(LexemeType.LeftSquareBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.Dot));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Semicolon:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));

                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.Q:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Assign));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Assign);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.Read:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Read));


                            //Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Read);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Write:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Write));

                            Generator.Push(GeneratorTask.Write);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.If:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.K));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.If));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task3);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.While:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.While));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task5);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task4);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.A:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Assign));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Assign);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.Read:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Read));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Read);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Write:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.Semicolon));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.Write));

                            Generator.Push(GeneratorTask.Write);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.If:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.K));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.If));

                            Generator.Push(GeneratorTask.Task3);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.While:
                        {
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.C));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));
                            Magazine.Push(new MagazineItem(LexemeType.While));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task5);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task1);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task4);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.H:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.LeftSquareBracket:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.RightSquareBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftSquareBracket));

                            Generator.Push(GeneratorTask.Index);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.C:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.LeftRoundBracket:
                        {
                            Magazine.Push(new MagazineItem(State.L));
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.L));
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.IntNumber:
                        {
                            Magazine.Push(new MagazineItem(State.L));
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.IntNumber);
                            break;
                        }
                        case LexemeType.FloatNumber:
                        {
                            Magazine.Push(new MagazineItem(State.L));
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.FloatNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.FloatNumber);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.L:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Less:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Less));

                            Generator.Push(GeneratorTask.Less);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.More:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.More));

                            Generator.Push(GeneratorTask.More);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Equal:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.Equal));

                            Generator.Push(GeneratorTask.Equal);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.LessOrEqual:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LessOrEqual));

                            Generator.Push(GeneratorTask.LessOrEqual);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.MoreOrEqual:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.MoreOrEqual));

                            Generator.Push(GeneratorTask.MoreOrEqual);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.NotEqual:
                        {
                            Magazine.Push(new MagazineItem(State.Z));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.NotEqual));

                            Generator.Push(GeneratorTask.NotEqual);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.K:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Else:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.RightBrace));
                            Magazine.Push(new MagazineItem(State.Q));
                            Magazine.Push(new MagazineItem(State.A));
                            Magazine.Push(new MagazineItem(LexemeType.LeftBrace));
                            Magazine.Push(new MagazineItem(LexemeType.Else));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Task2);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.E:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.LeftRoundBracket:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.IntNumber:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.IntNumber);
                            break;
                        }
                        case LexemeType.FloatNumber:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.FloatNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.FloatNumber);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.U:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Plus:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.T));
                            Magazine.Push(new MagazineItem(LexemeType.Plus));

                            Generator.Push(GeneratorTask.Plus);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Minus:
                        {
                            Magazine.Push(new MagazineItem(State.U));
                            Magazine.Push(new MagazineItem(State.T));
                            Magazine.Push(new MagazineItem(LexemeType.Minus));

                            Generator.Push(GeneratorTask.Minus);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.T:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.LeftRoundBracket:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.IntNumber:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.IntNumber);
                            break;
                        }
                        case LexemeType.FloatNumber:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(LexemeType.FloatNumber));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.FloatNumber);
                            break;
                        }
                        default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.V:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.Multiply:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(State.F));
                            Magazine.Push(new MagazineItem(LexemeType.Multiply));

                            Generator.Push(GeneratorTask.Multiply);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Divide:
                        {
                            Magazine.Push(new MagazineItem(State.V));
                            Magazine.Push(new MagazineItem(State.F));
                            Magazine.Push(new MagazineItem(LexemeType.Divide));

                            Generator.Push(GeneratorTask.Divide);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                    break;
                }
                case State.F:
                {
                    switch (current_lexeme.lexeme_type)
                    {
                        case LexemeType.LeftRoundBracket:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.RightRoundBracket));
                            Magazine.Push(new MagazineItem(State.E));
                            Magazine.Push(new MagazineItem(LexemeType.LeftRoundBracket));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.Empty);
                            break;
                        }
                        case LexemeType.Id:
                        {
                            Magazine.Push(new MagazineItem(State.H));
                            Magazine.Push(new MagazineItem(LexemeType.Id));

                            Generator.Push(GeneratorTask.Empty);
                            Generator.Push(GeneratorTask.VariableId);
                            break;
                        }
                        case LexemeType.IntNumber:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.IntNumber));

                            Generator.Push(GeneratorTask.IntNumber);
                            break;
                        }
                        case LexemeType.FloatNumber:
                        {
                            Magazine.Push(new MagazineItem(LexemeType.FloatNumber));

                            Generator.Push(GeneratorTask.FloatNumber);
                            break;
                        }
                            case LexemeType.ArrayInt:
                                {
                                    Magazine.Push(new MagazineItem(LexemeType.ArrayInt));

                                    Generator.Push(GeneratorTask.ArrayInt);
                                    break;
                                }
                            case LexemeType.ArrayFloat:
                                {
                                    Magazine.Push(new MagazineItem(LexemeType.ArrayFloat));

                                    Generator.Push(GeneratorTask.ArrayFloat);
                                    break;
                                }
                            default:
                        {
                            msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                            throw new Exception(msg);
                        }
                    }
                    break;
                }
                case State.Z:
                {
                    break;
                }
                case State.Error:
                default:
                    msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                    throw new Exception(msg);
            }
        }

        private void run_task()
        {
            string msg = "";
            int j = 0;
            switch (current_task)
            {
                case GeneratorTask.Empty:
                    break;
                case GeneratorTask.VariableId:
                    data.ops.Add(new OpsItem(current_lexeme.Value, current_lexeme));
                    break;
                case GeneratorTask.IntNumber:
                {
                    int num = current_lexeme.N;
                    data.ops.Add(new OpsItem(num, current_lexeme));  
                    break;
                }
                case GeneratorTask.FloatNumber:
                {
                    double num = current_lexeme.F;
                    data.ops.Add(new OpsItem(num, current_lexeme));
                    break;
                }
                case GeneratorTask.ArrayInt:
                {
                    int num = current_lexeme.N;
                    data.ops.Add(new OpsItem(num, current_lexeme.Value, true));
                    break;
                }
                case GeneratorTask.ArrayFloat:
                {
                    double num = current_lexeme.F;
                    data.ops.Add(new OpsItem(num, current_lexeme.Value, true));
                    break;
                }
                case GeneratorTask.Read:
                    data.ops.Add(new OpsItem(OpsItemOperation.Read, current_lexeme));
                    break;
                case GeneratorTask.Write:
                    data.ops.Add(new OpsItem(OpsItemOperation.Write, current_lexeme));
                    break;
                case GeneratorTask.Plus:
                    data.ops.Add(new OpsItem(OpsItemOperation.Plus, current_lexeme));
                    break;
                case GeneratorTask.Minus:
                    data.ops.Add(new OpsItem(OpsItemOperation.Minus, current_lexeme));
                    break;
                case GeneratorTask.Multiply:
                    data.ops.Add(new OpsItem(OpsItemOperation.Miltiply, current_lexeme));
                    break;
                case GeneratorTask.Divide:
                    data.ops.Add(new OpsItem(OpsItemOperation.Divide, current_lexeme));
                    break;
                case GeneratorTask.Less:
                    data.ops.Add(new OpsItem(OpsItemOperation.Less, current_lexeme));
                    break;
                case GeneratorTask.Assign:
                    data.ops.Add(new OpsItem(OpsItemOperation.Assign, current_lexeme));
                    break;
                case GeneratorTask.More:
                    data.ops.Add(new OpsItem(OpsItemOperation.More, current_lexeme));
                    break;
                case GeneratorTask.Equal:
                    data.ops.Add(new OpsItem(OpsItemOperation.Equal, current_lexeme));
                    break;
                case GeneratorTask.LessOrEqual:
                    data.ops.Add(new OpsItem(OpsItemOperation.LessOrEqual, current_lexeme));
                    break;
                case GeneratorTask.MoreOrEqual:
                    data.ops.Add(new OpsItem(OpsItemOperation.MoreOrEqual, current_lexeme));
                    break;
                case GeneratorTask.NotEqual:
                    data.ops.Add(new OpsItem(OpsItemOperation.NotEqual, current_lexeme));
                    break;
                case GeneratorTask.Index:
                    data.ops.Add(new OpsItem(OpsItemOperation.Index, current_lexeme));
                    break;
                case GeneratorTask.Task1:
                {
                    Marks.Push(new MarkerName("m1"));
                    MarkerName newMark = Marks.Peek();
                    data.ops.Add(new OpsItem(newMark, current_lexeme.Line));
                    data.ops.Add(new OpsItem(OpsItemOperation.JumpIfFalse, current_lexeme));
                    break;
                }
                case GeneratorTask.Task2:
                {
                    MarkerName place = Marks.Peek(); 
                    Marks.Pop();
                    Marks.Push(new MarkerName("m2"));
                    MarkerName newMark = Marks.Peek();
                    data.ops.Add(new OpsItem(newMark, current_lexeme.Line));
                    data.ops.Add(new OpsItem(OpsItemOperation.Jump, current_lexeme));
                    data.ops.Where(p => p.metka == place).First().pos = data.ops.Count;
                    break;
                }
                case GeneratorTask.Task3:
                {
                    MarkerName place = Marks.Peek();
                    Marks.Pop();
                    data.ops.Where(p => p.metka == place).First().pos = data.ops.Count;
                    break;
                }
                case GeneratorTask.Task4:
                {
                    Marks.Push(new MarkerName("m3"));
                        break;
                }
                case GeneratorTask.Task5:
                {
                    MarkerName place = Marks.Peek();
                    Marks.Pop();
                    Marks.Push(new MarkerName("m4"));
                    MarkerName newMark = Marks.Peek();
                        data.ops.Add(new OpsItem(newMark, current_lexeme.Line, data.ops.Count - 15));
                    data.ops.Add(new OpsItem(OpsItemOperation.Jump, current_lexeme));
                    data.ops.Where(p => p.metka == place).First().pos = data.ops.Count;
                    break;
                }
                default:
                {
                    msg = $"Ошибка генератора; Строка: {Convert.ToString(current_lexeme.Line)},позиция: {Convert.ToString(current_lexeme.Position)}";
                    throw new Exception(msg);
                }
            }
        }
    }
}

