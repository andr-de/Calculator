using System;

namespace CalculatorBack
{
    public enum ErrorType
    {
        ClosingBrakets,
        UnexpectedOperator,
        SecondPoint,
        UnsopportedSymbol,
        NotEnoughOperators,
        NotEnoughOperands,
        OpeningBrakets
    }
    public class ErrorClass
    {
        ErrorType typeOfError;
        char charOfError;
        int posOfError;
        public ErrorClass() { }

        public ErrorClass(ErrorType t, char c, int p)
        {
            typeOfError = t;
            charOfError = c;
            posOfError = p;
        }

        public ErrorClass(ErrorType t) : this(t, '\0', 0)
        {
        }

        public ErrorClass(ErrorType t, int p) : this(t, '\0', p)
        {
        }

        public string PrintError() => typeOfError switch
        {
            ErrorType.ClosingBrakets => String.Concat("Error: closing braket doesnt have an open braket at postition ", posOfError, "! Check your brakets, no more brakets errors will be shown\n"),
            ErrorType.UnexpectedOperator => String.Concat("Error: unexpected '", charOfError, "' operator at position ", posOfError, '\n'),
            ErrorType.SecondPoint => String.Concat("Error: second or more point in number at position ", posOfError, '\n'),
            ErrorType.NotEnoughOperands => String.Concat("Error: not enough operands for operator '", charOfError, "' operator at position ", posOfError, '\n'),
            ErrorType.NotEnoughOperators => String.Concat("Error: not enough opertors\n"),
            ErrorType.OpeningBrakets => String.Concat("Error: ", posOfError, " more closing brakets expected\n"),
            _ => String.Concat("Error: unsupported symbol '", charOfError, "' at position ", posOfError, '\n')
        };
    }
}
