# BP-Trains
The code is in C#

The entry point and input parser are in BP-Trains/Program.cs; the program accepts an input file in a format specified in assignment pdf and outputs the results of two different solvers in the format suggested in the task description (pls note that the example output provided along with the assigment does not seem to match the sample inputs; but I had kept to suggested format nevertheless)

The actual solvers and in the BP-Trains/MailTrainsSystem.cs; there is a simple one with one-by-one delivery, which I've made first to test the input parsing, path finding algo, etc; and another one with some improvements to make it more robust. There are also comments in a body of a third solver method in the same class with my thoughts on how it can be improved further, but which I, unfortunately, had no time to try out to implement - but please do take a look

The rest of the *.cs files are PODs and carry no logic of their own.

The text files included with the project contain some sample inputs which I used to test the code, along with their corresponding output
