using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace CalculatorBack.Test
{
    public class ComputeExpressionTest
    {
        [Fact]
        public void SingleOperation()
        {
            // Arrange
            var exp = new ExpressionProcessor();
            string add = "1+5";
            string minus = "1-5";
            string multiply = "2*5";
            string devision = "8/4";

            // Act
            double addResult = exp.ComputeExpression(add);
            double minusResult = exp.ComputeExpression(minus);
            double multiplyResult = exp.ComputeExpression(multiply);
            double devisionResult = exp.ComputeExpression(devision);

            // Assert
            Assert.Equal(6.0, addResult);
            Assert.Equal(-4.0, minusResult);
            Assert.Equal(10.0, multiplyResult);
            Assert.Equal(2.0, devisionResult);
        }

        [Fact]
        public void ToMuchClosingBrakets()
        {
            // Arrange
            string soloBraket = "123)+23";
            string excessBraket = "(1+1)*(3+2))+24";
            var exp = new ExpressionProcessor();
            var exp2 = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(soloBraket);
            exp2.ComputeExpression(excessBraket);
            bool soloB = exp.GetErrorSize() == 1;
            bool excessB = exp2.GetErrorSize() == 1;

            // Assert
            Assert.True(soloB);
            Assert.True(excessB);
            Assert.Equal((new ErrorClass(ErrorType.ClosingBrakets, 4)).PrintError(), exp.GetError(0).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.ClosingBrakets, 12)).PrintError(), exp2.GetError(0).PrintError());
        }

        [Fact]
        public void UnexpectedOperator()
        {
            // Arrange
            string unexpectedOperators = "*2+(/4)+(-5)+(5*)+*5";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(unexpectedOperators);

            // Assert
            Assert.True(exp.GetErrorSize() == 4);
            Assert.Equal((new ErrorClass(ErrorType.UnexpectedOperator, '*', 1)).PrintError(), exp.GetError(0).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.UnexpectedOperator, '/', 5)).PrintError(), exp.GetError(1).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.UnexpectedOperator, '*', 16)).PrintError(), exp.GetError(2).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.UnexpectedOperator, '*', 19)).PrintError(), exp.GetError(3).PrintError());
        }

        [Fact]
        public void SecondPoint()
        {
            // Arrange
            string secondPoint = "25,23,23+245,23,13";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(secondPoint);

            // Assert
            Assert.True(exp.GetErrorSize() == 2);
            Assert.Equal((new ErrorClass(ErrorType.SecondPoint, 6)).PrintError(), exp.GetError(0).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.SecondPoint, 16)).PrintError(), exp.GetError(1).PrintError());
        }

        [Fact]
        public void UnsupportedSymbol()
        {
            // Arrange
            string unsupportedSymbol = "25a+24v";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(unsupportedSymbol);

            // Assert
            Assert.True(exp.GetErrorSize() == 2);
            Assert.Equal((new ErrorClass(ErrorType.UnsopportedSymbol, 'a', 3)).PrintError(), exp.GetError(0).PrintError());
            Assert.Equal((new ErrorClass(ErrorType.UnsopportedSymbol, 'v', 7)).PrintError(), exp.GetError(1).PrintError());
        }

        [Fact]
        public void ToMuchOpeningBrakets()
        {
            // Arrange
            string openingBrakets = "25+5*(25+23*(35+5)+(35";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(openingBrakets);

            // Assert
            Assert.True(exp.GetErrorSize() == 1);
            Assert.Equal((new ErrorClass(ErrorType.OpeningBrakets, 2)).PrintError(), exp.GetError(0).PrintError());
        }

        [Fact]
        public void NotEnoughOperators()
        {
            // Arrange
            string notEnoughOperators = "24+52(242)+24";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(notEnoughOperators);

            // Assert
            Assert.True(exp.GetErrorSize() == 1);
            Assert.Equal((new ErrorClass(ErrorType.NotEnoughOperators)).PrintError(), exp.GetError(0).PrintError());
        }

        [Fact]
        public void InfiniteValues()
        {
            // Arrange
            string positiveInfinite = "1/0";
            string negativeInfinite = "-1/0";
            var exp = new ExpressionProcessor();
            var exp2 = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(positiveInfinite);
            exp2.ComputeExpression(negativeInfinite);

            // Assert
            Assert.True(exp.GetErrorSize() == 0);
            Assert.True(exp.FirstInfPosition == 2);
            Assert.True(exp2.GetErrorSize() == 0);
            Assert.True(exp2.FirstInfPosition == 3);
            Assert.True(Double.IsPositiveInfinity(exp.Result));
            Assert.True(Double.IsNegativeInfinity(exp2.Result));
        }

        [Fact]
        public void NaNValues()
        {
            // Arrange
            string NaN = "0/0";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(NaN);

            // Assert
            Assert.True(exp.GetErrorSize() == 0);
            Assert.True(exp.FirstNaNPosition == 2);
            Assert.True(Double.IsNaN(exp.Result));
        }

        [Fact]
        public void UnaryMinus()
        {
            // Arrange
            string unaryMinus = "-5*(-5)";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(unaryMinus);

            // Assert
            Assert.True(exp.GetErrorSize() == 0);
            Assert.Equal(25.0, exp.Result);
        }

        [Fact]
        public void PointedNumber()
        {
            // Arrange
            string pointedNumbers = "0,5+(1,5*3)";
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(pointedNumbers);

            // Assert
            Assert.True(exp.GetErrorSize() == 0);
            Assert.Equal(5.0, exp.Result);
        }

        [Fact]
        public void OverInfiniteNumber()
        {
            // Arrange
            var overInfiniteNumber = new StringBuilder();
            for (int i = 0; i < 50; i++)
                overInfiniteNumber.Append("1111111111111111111111111111111111111111111");
            overInfiniteNumber.Append("+25");
            var exp = new ExpressionProcessor();

            // Act
            exp.ComputeExpression(overInfiniteNumber.ToString());

            // Assert
            Assert.True(exp.GetErrorSize() == 0);
            Assert.True(Double.IsPositiveInfinity(exp.Result));
        }

        [Fact]
        public void SomePositiveTests()
        {
            var exp = new ExpressionProcessor();
            string sp1 = "1+2*(3)*(-5)";
            string sp2 = "2+(6+1)*2/(1-2*4)";
            string sp3 = "-5/(1-2*(5,5-5,5))";

            exp.ComputeExpression(sp1);
            Assert.True(exp.GetErrorSize() == 0);
            Assert.Equal(-29, exp.Result);

            exp.ComputeExpression(sp2);
            Assert.True(exp.GetErrorSize() == 0);
            Assert.Equal(0, exp.Result);

            exp.ComputeExpression(sp3);
            Assert.True(exp.GetErrorSize() == 0);
            Assert.Equal(-5, exp.Result);
        }
    }
}
