using System;
using System.Collections.Generic;
using System.Text;

namespace CalculatorBack
{
    public class ExpressionProcessor
    {
        private List<ErrorClass> errors = new List<ErrorClass>();
        private Stack<double> numbers = new Stack<double>();
        private Stack<(char operation, int position)> operators = new Stack<(char operation, int position)>();
        public string Expr { get; private set; }
        public double Result { get; private set; }
        public int FirstInfPosition { get; private set; }
        public int FirstNaNPosition { get; private set; }

        public ExpressionProcessor() { }

        public ExpressionProcessor(string expression) { Expr = expression; }

        // Первичная проверка на корректность строки. Проверяем правильность скобочной последовательности,
        // отсутствие нескольких символов операций подряд, лишних разделителей в числах и посторонних символов
        public bool IsProbablyGood()
        {
            int i = 0;
            int exprlen = Expr.Length;
            // -1 так как нужно проигнорировать добавленную нами первую скобку, 
            // чтобы не ломалось позиционирование ошибки закрывающих скобок
            int openbrackets = -1;
            int closebrackets = 0;
            bool bracketsreported = false;

            while (i < exprlen)
            {
                char currchar = Expr[i];
                if (currchar == ')')
                {
                    closebrackets++;
                    if ((closebrackets > openbrackets) && (!bracketsreported) && (i != exprlen - 1))
                    {
                        errors.Add(new ErrorClass(ErrorType.ClosingBrakets, i));
                        bracketsreported = true;
                    }
                    i++;
                }
                else if (currchar == '(')
                {
                    openbrackets++;
                    i++;
                }
                else if (IsOperand(currchar))
                {
                    // Символ опирации считается некорректным, если идёт после открывающей скобки и не является минусом,
                    // если идёт перед закрывающей скобкой
                    // или если идёт сразу после другого символа операции.
                    if (((Expr[i - 1] == '(') && (currchar != '-')) ||
                        Expr[i + 1] == ')' ||
                        IsOperand(Expr[i - 1]))
                    {
                        errors.Add(new ErrorClass(ErrorType.UnexpectedOperator, currchar, i));
                    }
                    i++;
                }
                else if (char.IsDigit(currchar))
                {
                    bool pointed = false;
                    while (char.IsDigit(Expr[i]) || (Expr[i] == ','))
                    {
                        if (Expr[i] == ',')
                            if (!pointed)
                                pointed = true;
                            else
                                errors.Add(new ErrorClass(ErrorType.SecondPoint, i));
                        i++;
                        if (i >= exprlen)
                            break;
                    }
                }
                else
                {
                    errors.Add(new ErrorClass(ErrorType.UnsopportedSymbol, currchar, i));
                    i++;
                }
            }
            // +1 так как нужно всё же учесть добавленные нами скобки
            if ((openbrackets + 1 > closebrackets) && (!bracketsreported))
            {
                errors.Add(new ErrorClass(ErrorType.OpeningBrakets, openbrackets + 1 - closebrackets));
            }
            return errors.Count == 0;
        }

        // Вычисляем выражение. Попутно могут быть обнаружены ошибки, отличные от тех, что искали в IsProbablyGood.
        // А именно: если для операторов не хватает операндов или наоборот.
        // При этом, если что-то нашли - останавливаемся. Так как я указываю помимо ошибки её место в изначальном выражении,
        // то попытка игнорировать какие-то символы или "додумывать", что должно было быть, легко может привести
        // к некорректному определению того, в каком именно операнде ошибка.
        public double ComputeExpression(string ex)
        {
            // Выражение оборачивается в скобки, в дальнейшем это сильно упростит определение унарного минуса
            Expr = String.Concat("(", ex, ")");
            // Если ExprProcessor уже что-то до этого обрабатывал - сбрасываем значения полей на стартовые 
            ClearInfo();

            if (!IsProbablyGood())
            {
                Result = 0;
                return Result;
            }

            int exprlen = Expr.Length;
            int i = 0;
            while ((i < exprlen) && (errors.Count == 0))
            {
                char currchar = Expr[i];
                if (char.IsDigit(currchar))
                {
                    HandleNumber(ref i);
                }
                else
                {
                    if (currchar == '(')
                        operators.Push((currchar, i));
                    else if (currchar == ')')
                    {
                        HandleCloseBraket();
                    }
                    // Благодаря тому, что изначальное выражение оборачивается в скобки, так однозначно определяется унарный минус
                    else if ((currchar == '-') && (Expr[i - 1] == '('))
                        operators.Push(('~', i));
                    else if (operators.Count == 0)
                        operators.Push((currchar, i));
                    else
                    {
                        HandleOperator(currchar, i);
                    }
                    i++;
                }
            }
            // Если остались невычесленные операции, 
            while ((operators.Count != 0) && (errors.Count == 0))
            {
                if (numbers.TryPop(out double a) && numbers.TryPop(out double b))
                    CheckForUnaryMinus(PerformOperation(a, b, operators.Pop()));
                else
                    errors.Add(new ErrorClass(ErrorType.NotEnoughOperands, operators.Peek().operation, operators.Pop().position));
            }

            if ((operators.Count == 0) && (numbers.Count > 1))
                errors.Add(new ErrorClass(ErrorType.NotEnoughOperators));

            if (errors.Count == 0)
            {
                Result = numbers.Peek();
                return Result; ;
            }
            Result = 0;
            return Result; ;
        }

        static bool IsOperand(char c)
        {
            return (c == '+') || (c == '-') || (c == '*') || (c == '/');
        }

        // Определение приоретата операндов. Несмотря на то, что скобки - технически не операнд, им тоже необходим приоритет.
        // Используется при определении последовательности действий.
        static int Priority(char c) => c switch
        {
            '(' => 0,
            ')' => 0,
            '+' => 1,
            '-' => 1,
            '*' => 2,
            '/' => 2,
            '~' => 3,
            _ => -1
        };

        // Первое и второе число в аргументах идут в обратном порядке осозннанно. 
        // Это связано с тем, что в основном коде в качестве аргументов идут вызовы метода Pop()
        // от соответствующего стека. Тем самым числа получаются в обратном порядке.
        // Дополнительно отмечаем, если в процессе вычисления выражения появились бесконечности или NaN
        double PerformOperation(double secondNumber, double firstNumber, (char operation, int position) c)
        {
            var result = c.operation switch
            {
                '+' => firstNumber + secondNumber,
                '-' => firstNumber - secondNumber,
                '*' => firstNumber * secondNumber,
                _ => firstNumber / secondNumber,
            };
            if (Double.IsInfinity(result) && (FirstInfPosition == -1))
                FirstInfPosition = c.position;
            else if (Double.IsNaN(result) && (FirstNaNPosition == -1))
                FirstNaNPosition = c.position;
            return result;
        }

        public string CombineErrors()
        {
            var result = new StringBuilder();
            foreach (var i in errors)
                result.Append(i.PrintError());
            return result.ToString();
        }

        public int GetErrorSize()
        {
            return errors.Count;
        }

        public ErrorClass GetError(int pos)
        {
            if (pos < errors.Count)
                return errors[pos];
            else
                return null;

        }

        // Если в момент добавления числа в стек, в стеке операторов наверху лежит унарный минус,
        // то это число должно быть добавлено с обратным знаком.
        public void CheckForUnaryMinus(double number)
        {
            if (operators.TryPeek(out var operationStackTop) && (operationStackTop.operation == '~'))
            {
                numbers.Push(-number);
                operators.Pop();
            }
            else
                numbers.Push(number);
        }

        // Считывание числа из строки выражения. Благодаря IsProbablyGood() уверены, что все числа должны корректно парсится
        public void HandleNumber(ref int currentIndex)
        {
            var tempDouble = new StringBuilder();
            char currentSymbol = Expr[currentIndex];
            while (char.IsDigit(currentSymbol) || (currentSymbol == ','))
            {
                tempDouble.Append(currentSymbol);
                currentIndex++;
                if (currentIndex >= Expr.Length)
                    break;
                currentSymbol = Expr[currentIndex];
            }
            var number = Double.Parse(tempDouble.ToString());
            CheckForUnaryMinus(number);
        }

        // Обработка закрывающей скобки. Необходимо выполнить все операции, соответствующие этой скобке и её парной.
        public void HandleCloseBraket()
        {
            var operationStackTop = operators.Pop();
            while (operationStackTop.operation != '(')
            {
                if (numbers.TryPop(out double a) && numbers.TryPop(out double b))
                    CheckForUnaryMinus(PerformOperation(a, b, operationStackTop));
                else
                {
                    errors.Add(new ErrorClass(ErrorType.NotEnoughOperands, operationStackTop.operation, operationStackTop.position));
                    break;
                }
                operationStackTop = operators.Pop();
            }
            CheckForUnaryMinus(numbers.Pop());
        }

        // Обработка оператора. Пока его приоритет <= верхнего приоритета в стеке,
        // оператор из стека можно выполнить.
        public void HandleOperator(char operation, int position)
        {
            var operationStackTop = operators.Peek();
            while (Priority(operation) <= Priority(operationStackTop.operation))
            {
                if (numbers.TryPop(out double a) && numbers.TryPop(out double b))
                    CheckForUnaryMinus(PerformOperation(a, b, operators.Pop()));
                else
                {
                    errors.Add(new ErrorClass(ErrorType.NotEnoughOperands, operationStackTop.operation, operationStackTop.position));
                    break;
                }
                if (!operators.TryPeek(out operationStackTop))
                    break;
            }
            operators.Push((operation, position));
        }

        private void ClearInfo()
        {
            errors.Clear();
            numbers.Clear();
            operators.Clear();
            FirstInfPosition = -1;
            FirstNaNPosition = -1;
        }
    }
}
