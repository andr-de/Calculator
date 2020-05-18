using System;
using System.Text;
using Xunit;

namespace CalculatorBack.Test
{
    public class PreProcessReceivedStringTest
    {
        [Fact]
        public void DifferentBrakets()
        {
            // Arrange
            var differentBrakets = "{a}+[b]+(c)";

            // Act
            var processedString = Program.PreProcessReceivedString(differentBrakets);

            // Assert
            Assert.Equal("(a)+(b)+(c)", processedString);
        }

        [Fact]
        public void BraketsCollapse()
        {
            // Arrange
            var differentBrakets = "(()(()))";

            // Act
            var processedString = Program.PreProcessReceivedString(differentBrakets);

            // Assert
            Assert.Equal("", processedString);
        }

        [Fact]
        public void SpacesTrim()
        {
            // Arrange
            var spacesTrim = " 112 + 3 ";

            // Act
            var processedString = Program.PreProcessReceivedString(spacesTrim);

            // Assert
            Assert.Equal("112+3", processedString);
        }

        [Fact]
        public void AddSomeMultiply()
        {
            // Arrange
            var differentBrakets = "1(2)3+(4)(5)+1,(2),3";

            // Act
            var processedString = Program.PreProcessReceivedString(differentBrakets);

            // Assert
            Assert.Equal("1*(2)*3+(4)*(5)+1,*(2)*,3", processedString);
        }
    }
}
