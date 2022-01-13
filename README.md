# BP-Trains
UPDATE: Added a third 'greedy trains' solver, based on the ideas earlier stated in comments

The entry point and input parsers are in BP-Trains/Program.cs; the program accepts an input file in a format specified in assignment pdf and outputs the results of two different solvers in the format suggested in the task description (pls note that the example output provided along with the assigment does not seem to match the sample inputs; but I had kept to suggested format nevertheless)

The actual solvers are in BP-Trains/MailTrainsSystem.cs; there is a simple one with one-by-one delivery, which I've made first to test the input parsing, path finding algo, etc; another one with slight improvements to make the first algo more robust, and the third one, working in a different fashion and much better suited for larger trains and more packages.

The rest of the *.cs files are PODs and carry no logic of their own.

The text files included with the project contain some sample inputs which I used to test the code, along with their corresponding output

