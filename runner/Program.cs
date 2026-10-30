using System;
Action a = null;
Action f1 = () => Console.Write("1");
Action f2 = () => Console.Write("2");
a += f1;
a += f2;
a += f1;
a -= f1;
a();