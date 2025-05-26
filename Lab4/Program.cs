using System;
using System.Collections.Generic;
using System.Linq;

namespace TransportNW
{
    internal class Program
    {
        private static int[,] NorthWestCorner(
            int[,] c,         // матриця витрат (m × n)
            int[] supplyInit, // PO (m)
            int[] demandInit) // PN (n)
        {
            int m = supplyInit.Length, n = demandInit.Length;
            int[,] x = new int[m, n];                // план, 0 = порожньо
            int[] supply = supplyInit.ToArray();     // робочі копії
            int[] demand = demandInit.ToArray();
            var sequence = new List<string>();       // послідовність заповнення

            int i = 0, j = 0;
            while (i < m && j < n)
            {
                int alloc = Math.Min(supply[i], demand[j]);
                x[i, j] = alloc;
                sequence.Add($"(x{i + 1}{j + 1} = {alloc})");

                supply[i] -= alloc;
                demand[j] -= alloc;

                // виродження: обнулити сусідню клітинку, аби зберегти (m+n-1) базисних
                if (supply[i] == 0 && demand[j] == 0 && i + 1 < m)
                    x[i + 1, j] = 0;

                if (supply[i] == 0) i++;
                if (demand[j] == 0) j++;
            }

            // ДРУК ПРОТОКОЛУ
            Console.WriteLine("\nМатриця витрат:");
            PrintMatrix(c, "c");

            Console.WriteLine("Вектор запасів:");
            PrintVector(supplyInit, "PO");

            Console.WriteLine("Вектор заявок:");
            PrintVector(demandInit, "PN");
            Console.WriteLine();

            Console.WriteLine("Пошук опорного плану перевезень методом північно‑західного кута:\n");
            Console.WriteLine("Послідовність заповнення таблиці:");
            Console.WriteLine(string.Join(" -> ", sequence) + "\n");

            Console.WriteLine("Опорний план перевезень:");
            PrintPlan(x);

            // обчислюємо собівартість
            int cost = 0;
            var formula = new List<string>();
            for (int r = 0; r < m; r++)
                for (int s = 0; s < n; s++)
                    if (x[r, s] > 0)
                    {
                        cost += x[r, s] * c[r, s];
                        formula.Add($"{x[r, s]} * {c[r, s]}");
                    }

            Console.WriteLine("Вартість перевезень за опорним планом:");
            Console.WriteLine($"S = {string.Join(" + ", formula)} = {cost}\n");

            return x; 
        }

        // процедури друку 
        private static void PrintMatrix(int[,] m, string name)
        {
            int rows = m.GetLength(0), cols = m.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                Console.Write(i == 0 ? $"{name} := (" : "       ");
                for (int j = 0; j < cols; j++)
                {
                    Console.Write($"{m[i, j],3}");
                    if (j < cols - 1) Console.Write(" ");
                }
                Console.WriteLine(i == rows - 1 ? " )" : "");
            }
            Console.WriteLine();
        }

        private static void PrintVector(int[] v, string name)
        {
            Console.Write($"{name} := ( ");
            Console.Write(string.Join(" ", v));
            Console.WriteLine(" )\n");
        }

        private static void PrintPlan(int[,] plan)
        {
            int m = plan.GetLength(0), n = plan.GetLength(1);
            for (int i = 0; i < m; i++)
            {
                Console.Write(i == 0 ? "X := (" : "     ");
                for (int j = 0; j < n; j++)
                {
                    string cell = plan[i, j] == 0 ? "  x" : plan[i, j].ToString().PadLeft(3);
                    Console.Write(cell);
                    if (j < n - 1) Console.Write(" ");
                }
                Console.WriteLine(i == m - 1 ? " )" : "");
            }
            Console.WriteLine();
        }


        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; 

            // Дані варіанта 20
            int[,] cost =
            {
                { 7,  8, 10, 15 },
                { 5, 6,  9, 10 },
                { 4,  7, 10, 8 }
            };
            int[] PO = { 80, 90, 60 };
            int[] PN = { 75, 65, 40, 50}; 

            // Виконуємо лише NW‑кут
            NorthWestCorner(cost, PO, PN);

            Console.WriteLine("Натисніть Enter, щоб завершити ...");
            Console.ReadLine();
        }
    }
}




























