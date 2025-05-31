using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translator
{
    internal class OpsInterpretator
    {
        private InterpretData input_data;
        public OpsInterpretator(InterpretData data)
        {
            input_data = data;
        }

        public void Run()
        {
            List<OpsItem> global = new List<OpsItem>();
            Stack<OpsItem> magazine = new Stack<OpsItem>();
            var ops = input_data.ops;

            for (var i = 0; i < ops.Count; ++i)
            {
                switch (ops[i].type)
                {
                    case OpsItemType.VariableName:
                    {
                        var item = global.Where(p => p.var_name == ops[i].var_name).FirstOrDefault();
                        if (item == null)
                        {
                            if (ops[i+1].type != OpsItemType.Operation) // read(z) - z !=
                            {
                                global.Add(ops[i]);
                                magazine.Push(ops[i]);
                            }

                        }
                        else
                        {
                            magazine.Push(item);
                        }

                        break;
                    }
                    case OpsItemType.IntNumber:
                    case OpsItemType.FloatNumber:
                    {
                        magazine.Push(ops[i]);
                        break;
                    }
                    case OpsItemType.Operation:
                    {
                        switch (ops[i].operation)
                        {
                            case OpsItemOperation.Assign:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                               
                                if (item != null)
                                {
                                    if (item.type == OpsItemType.ArrayFloat)
                                    {
                                        int idx = a.int_num;

                                        if (b.type == OpsItemType.IntNumber)
                                        {
                                            item.arrayFloat[idx] = b.int_num;
                                        }
                                        else
                                        {
                                            item.arrayFloat[idx] = b.float_num;
                                        }
                                    }

                                    else if (b.type == OpsItemType.IntNumber)
                                    {
                                        item.int_num = b.int_num;
                                        item.type = OpsItemType.IntNumber;
                                    }
                                    else
                                    {
                                        item.float_num = b.float_num;
                                        item.type = OpsItemType.FloatNumber;
                                    }

                                            if (item2 != null)
                                            {
                                                if (item.type == OpsItemType.FloatNumber)
                                                {
                                                    item.float_num = item2.arrayFloat[b.int_num];
                                                }
                                            }

                                            if (item.type == OpsItemType.ArrayFloat && item2.type == OpsItemType.ArrayFloat)
                                            {
                                                item.arrayFloat[a.int_num] = item2.arrayFloat[b.int_num];
                                            }


                                            magazine.Push(item);
                                }
                                else
                                {
                                    string msg = "Ошибка интерпретатора; Неизвестная переменная; строка:"
                                        + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                    throw new Exception(msg);   
                                }

                                break;
                            }
                            case OpsItemOperation.Read:
                            {
                                if (magazine.Count == 0)
                                {
                                        string msg = "Ошибка интерпретатора; Неизвестная переменная; строка:"
                                    + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                        throw new Exception(msg);
                                }

                                var a = magazine.Pop();

                                var item = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                if (item == null)
                                {  
                                    string msg = "Ошибка интерпретатора; Неизвестная переменная; строка:"
                                        + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                    throw new Exception(msg);                                        
                                }

                                if (item.type == OpsItemType.ArrayFloat)
                                {
                                    if (a.int_num == 0)
                                    {
                                       double result = 0;
                                       for (int j = 0; j < item.arrayFloat.Length; j++)
                                       {
                                           Console.Write($"{item.var_name}[{j}] = ");
                                            try
                                            {
                                                    result = Convert.T
                                                            oDouble(Console.ReadLine());
                                                    item.arrayFloat[j] = result;

                                            }
                                            catch { }
                                       }         
                                    }
                                    else
                                    {
                                        Console.Write($"{item.var_name}[{a.int_num}] = ");
                                        double result = Convert.ToDouble(Console.ReadLine());
                                        item.arrayFloat[a.int_num] = result;
                                    }              
                                }
                                else
                                {
                                    Console.Write("Введите значение " + item.var_name + ": ");

                                    if (item.type == OpsItemType.IntNumber)
                                    {
                                        int result = Convert.ToInt32(Console.ReadLine());
                                        item.int_num = result;
                                    }
                                    else
                                    {
                                        double result = Convert.ToDouble(Console.ReadLine());
                                        item.float_num = result;
                                    }
                                }
                                    
                                break;
                            }
                            case OpsItemOperation.Write:
                            {
                                var a = magazine.Pop();

                                var item = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                if (item != null)
                                {
                                    if (item.type == OpsItemType.ArrayFloat)
                                    {
                                        if (a.int_num != 0)
                                            Console.WriteLine(item.arrayFloat[a.int_num]);
                                        else
                                        {
                                            for (int j = 0; j < item.arrayFloat.Length; j++) {
                                                Console.Write($"{item.arrayFloat[j]} ");
                                            }
                                            Console.WriteLine();
                                        }
                                                    
                                    }
                                    else
                                    {
                                        a = item;
                                        if (a.type == OpsItemType.IntNumber)
                                        {
                                            Console.WriteLine(a.int_num);
                                        }
                                        else
                                        {
                                            Console.WriteLine(a.float_num);
                                        }
                                    }
                                }
                                else
                                {
                                    if (a.type == OpsItemType.IntNumber)
                                    {
                                        Console.WriteLine(a.int_num);
                                    }
                                    else
                                    {
                                        Console.WriteLine(a.float_num);
                                    }
                                }

                                    break;
                            }
                            case OpsItemOperation.Plus:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                double result = x + y;
                                magazine.Push(new OpsItem(result, a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.Minus:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                double result = x - y;
                                magazine.Push(new OpsItem(result, a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.Miltiply:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                double result = x * y;
                                magazine.Push(new OpsItem(result, a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.Divide:
                            {
                                try
                                {
                                    var b = magazine.Pop();
                                    var a = magazine.Pop();

                                    var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                    var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                    int idx1 = 0, idx2 = 0;
                                    if (a.type == OpsItemType.ArrayFloat)
                                        idx1 = a.int_num;
                                    if (b.type == OpsItemType.ArrayFloat)
                                        idx2 = b.int_num;

                                    if (item1 != null)
                                        a = item1;
                                    if (item2 != null)
                                        b = item2;

                                    double x = 0, y = 0;
                                    switch (a.type)
                                    {
                                        case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                        case OpsItemType.IntNumber: x = a.int_num; break;
                                        case OpsItemType.FloatNumber: x = a.float_num; break;
                                    }
                                    switch (b.type)
                                    {
                                        case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                        case OpsItemType.IntNumber: y = b.int_num; break;
                                        case OpsItemType.FloatNumber: y = b.float_num; break;
                                    }

                                    if (y == 0)
                                    {
                                        string msg = "Ошибка интерпретатора; Деление на ноль; строка:"
                                        + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                        throw new Exception(msg);
                                    }

                                    double result = x / y;
                                    magazine.Push(new OpsItem(result, a.line, a.pos));
                                }
                                catch
                                {
                                    string msg = "Ошибка интерпретатора; Деление на ноль; строка:"
                                    + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                    throw new Exception(msg);
                                }
                                break;
                            }
                            case OpsItemOperation.Less:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x < y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.More:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x > y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.Equal:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x == y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.LessOrEqual:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x <= y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.MoreOrEqual:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break;
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x >= y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.NotEqual:
                            {
                                var b = magazine.Pop();
                                var a = magazine.Pop();

                                var item1 = global.Where(p => p.var_name == a.var_name).FirstOrDefault();
                                var item2 = global.Where(p => p.var_name == b.var_name).FirstOrDefault();

                                int idx1 = 0, idx2 = 0;
                                if (a.type == OpsItemType.ArrayFloat)
                                    idx1 = a.int_num;
                                if (b.type == OpsItemType.ArrayFloat)
                                    idx2 = b.int_num;

                                if (item1 != null)
                                    a = item1;
                                if (item2 != null)
                                    b = item2;

                                double x = 0, y = 0;
                                switch (a.type)
                                {
                                    case OpsItemType.ArrayFloat: x = a.arrayFloat[idx1]; break;
                                    case OpsItemType.IntNumber: x = a.int_num; break;
                                    case OpsItemType.FloatNumber: x = a.float_num; break;
                                }
                                switch (b.type)
                                {
                                    case OpsItemType.ArrayFloat: y = b.arrayFloat[idx2]; break;
                                    case OpsItemType.IntNumber: y = b.int_num; break; 
                                    case OpsItemType.FloatNumber: y = b.float_num; break;
                                }

                                bool result = x != y;
                                magazine.Push(new OpsItem(Convert.ToInt32(result), a.line, a.pos));

                                break;
                            }
                            case OpsItemOperation.Jump:
                            {
                                var metka = magazine.Pop();

                                i = metka.pos - 1;
                                break;
                            }
                            case OpsItemOperation.JumpIfFalse:
                            {
                                var metka = magazine.Pop();
                                var a = magazine.Pop(); // результат сравнения

                                if (a.int_num == 0) // результат - ложь
                                {
                                    i = metka.pos - 1;
                                }
                                break;
                            }
                            case OpsItemOperation.Index:
                            {
                                var idx = magazine.Pop();
                                var arr = magazine.Pop();

                                var item = global.Where(p => p.var_name == arr.var_name).FirstOrDefault();

                                if (item.type == OpsItemType.VariableName)
                                {
                                    global.Remove(item);
                                    item.type = OpsItemType.ArrayFloat;

                                    item.arrayFloat = new double[idx.int_num];
                                    for (int j = 0; j < idx.int_num; j++)
                                    {
                                        item.arrayFloat[j] = 0;
                                    }

                                    magazine.Push(item);
                                    global.Add(item);
                                }
                                else
                                {
                                    if (idx.float_num != 0)
                                        magazine.Push(new OpsItem(0, Convert.ToInt32(idx.float_num), item.var_name, true));
                                    else
                                        magazine.Push(new OpsItem(0, idx.int_num, item.var_name, true));
                                }

                                break;
                            }
                            default:
                            {
                                string msg = "Ошибка интерпретатора; Неизвестная операция; строка: "
                                + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                                throw new Exception(msg);
                            }
                        }
                        break;
                    }
                    case OpsItemType.MarkerName:
                    {
                        magazine.Push(ops[i]);
                        break;
                    }
                    default:
                    {
                        string msg = "Ошибка интерпретатора; Неизвестный элемент ОПС; строка:"
                            + Convert.ToString(ops[i].line) + ", позиция: " + Convert.ToString(ops[i].pos);
                        throw new Exception(msg);
                    }
                }
            }
        }
    }
}