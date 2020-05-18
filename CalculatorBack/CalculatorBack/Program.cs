using System;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;

namespace CalculatorBack
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
                StartCalculator(args[0]);
            else
                Console.WriteLine("Sorry, communication pipe name needed");
        }

        private static void StartCalculator(string pipename)
        {
            using (var namedPipeClient = new NamedPipeClientStream(".", pipename, PipeDirection.InOut))
            {
                namedPipeClient.Connect();
                namedPipeClient.ReadMode = PipeTransmissionMode.Message;
                byte[] resultBytes;

                try
                {
                    string expression = ProcessReceivedExpression(namedPipeClient);

                    while (!expression.Equals("quit"))
                    {
                        var ep = new ExpressionProcessor();
                        string preProcessedExpression = PreProcessReceivedString(expression);
                        if (preProcessedExpression.Length == 0)
                            resultBytes = Encoding.Unicode.GetBytes("Your expression equivalent to empty, try another!");
                        else
                        {
                            ep.ComputeExpression(preProcessedExpression);
                            resultBytes = GetResponseBytes(ref ep);
                        }
                        namedPipeClient.Write(resultBytes, 0, resultBytes.Length);
                        expression = ProcessReceivedExpression(namedPipeClient);
                    }
                }
                catch
                {
                    resultBytes = Encoding.Unicode.GetBytes("Something went wrong, sorry!");
                    namedPipeClient.Write(resultBytes, 0, resultBytes.Length);
                }

                resultBytes = Encoding.Unicode.GetBytes("Good buy");
                namedPipeClient.Write(resultBytes, 0, resultBytes.Length);
            }
        }

        private static string ProcessReceivedExpression(NamedPipeClientStream namedPipeClient)
        {
            var expressionBuilder = new StringBuilder();

            do
            {
                byte[] expressionBuffer = new byte[6];
                namedPipeClient.Read(expressionBuffer, 0, expressionBuffer.Length);
                expressionBuilder.Append(Encoding.Unicode.GetString(expressionBuffer));
            }
            while (!namedPipeClient.IsMessageComplete);

            return expressionBuilder.ToString().Trim('\0', ' ');
        }

        // Обработка принятой строки. Унифицируем скобки, точки, умножение скобок
        public static string PreProcessReceivedString(string expression)
        {
            string result;
            result = expression.Replace('{', '(');
            result = result.Replace('[', '(');
            result = result.Replace('}', ')');
            result = result.Replace(']', ')');

            while (result.Contains("()"))
            {
                result = result.Replace("()", "");
            }

            result = result.Replace(" ", "");
            result = result.Replace('.', ',');
            result = result.Replace(")(", ")*(");
            result = Regex.Replace(result, @"(\d)\(", m => string.Format("{0}*(", m.Groups[1].Value));
            result = Regex.Replace(result, @",\(", ",*(");
            result = Regex.Replace(result, @"\)(\d)", m => string.Format(")*{0}", m.Groups[1].Value));
            result = Regex.Replace(result, @"\),", ")*,");
            return result;
        }

        private static byte[] GetResponseBytes(ref ExpressionProcessor epr)
        {
            // Если выражение некорректное или приняло значение бесконечности или NAN - отдельно собираем отправляемый result
            string result;
            if (epr.GetErrorSize() > 0)
            {
                result = String.Concat("Your trimmed expression: ", epr.Expr.Substring(1, epr.Expr.Length - 2), "\n");
                result = String.Concat(result, epr.CombineErrors());
            }
            else if (Double.IsNaN(epr.Result))
                result = String.Concat("Warning! Expression became Infinite while processing operand at position ",
                        epr.FirstInfPosition,
                        "\n",
                        "Warning! Expression became NaN while processing operand at position ",
                    epr.FirstNaNPosition,
                    "\n",
                    "Result: NaN");

            else if (Double.IsInfinity(epr.Result))
            {
                result = String.Concat("Warning! Expression became Infinite while processing operand at position ",
                        epr.FirstInfPosition,
                        "\n");

                if (Double.IsPositiveInfinity(epr.Result))
                    result = String.Concat(result, "Result: Infinite");
                else
                    result = String.Concat(result, "Result: -Infinite");
            }
            else
                result = epr.Result.ToString();
            return Encoding.Unicode.GetBytes(result);
        }
    }
}
