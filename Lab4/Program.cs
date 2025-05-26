using System;
using System.Collections.Generic;
using System.Linq;

namespace TransportNW
{
    internal class Program
    {
        //  МЕТОД ПІВНІЧНО‑ЗАХІДНОГО КУТА
        private static int[,] NorthWestCorner(
            int[,] c,         // матриця витрат (m × n)
            int[] supplyInit, // PO (m)
            int[] demandInit) // PN (n)
        {
            int m = supplyInit.Length, n = demandInit.Length;
            int[,] x = CreateEmptyPlan(m, n);               // план, 0 = порожньо
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

            Console.WriteLine("----Пошук опорного плану перевезень методом північно‑західного кута:----\n");
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



        //  МЕТОД МІНІМАЛЬНОГО ЕЛЕМЕНТА 

        private static int[,] LeastCost(int[,] c, int[] supplyInit, int[] demandInit)
        {
            int m = supplyInit.Length, n = demandInit.Length;
            int[,] x = CreateEmptyPlan(m, n);          
            int[] supply = supplyInit.ToArray();
            int[] demand = demandInit.ToArray();
            bool[] rowDone = new bool[m];
            bool[] colDone = new bool[n];
            var seq = new List<string>();

            // зібрати всі клітинки у список і відсортувати за вартістю
            var cells = new List<(int cost, int i, int j)>();
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    cells.Add((c[i, j], i, j));
            cells.Sort((a, b) => a.cost.CompareTo(b.cost));

            int remaining = m + n; // кількість ще не виключених рядків + стовпців
            while (remaining > 0)
            {
                // знайти першу клітинку, у якій і рядок, і стовпець ще активні
                var cell = cells.First(t => !rowDone[t.i] && !colDone[t.j]);
                int i = cell.i, j = cell.j;
                int alloc = Math.Min(supply[i], demand[j]);
                x[i, j] = alloc;
                seq.Add($"(c={cell.cost}) → x{i + 1}{j + 1} = {alloc}");

                supply[i] -= alloc;
                demand[j] -= alloc;

                if (supply[i] == 0 && !rowDone[i])
                {
                    rowDone[i] = true;
                    remaining--;
                    seq.Add($"Виключаємо з розгляду {i + 1}-й пункт відправлення");
                }
                if (demand[j] == 0 && !colDone[j])
                {
                    colDone[j] = true;
                    remaining--;
                    seq.Add($"Виключаємо з розгляду {j + 1}-й пункт призначення");
                }
            }

            int basicCount = 0;
            for (int r = 0; r < m; r++)
                for (int s = 0; s < n; s++)
                    if (x[r, s] > 0) basicCount++;
            if (basicCount < m + n - 1)
            {
                var empties = cells.Where(t => x[t.i, t.j] == 0).ToList();
                foreach (var e in empties)
                {
                    x[e.i, e.j] = 0; // позначаємо як базисну
                    basicCount++;
                    if (basicCount == m + n - 1) break;
                }
            }

         
            Console.WriteLine("---Пошук опорного плану (метод мінімального елемента):---\n");
            Console.WriteLine("Кроки алгоритму:");
            foreach (var s in seq) Console.WriteLine("  " + s);
            Console.WriteLine();
            Console.WriteLine("Опорний план перевезень:");
            PrintPlan(x);
            PrintCost(c, x, "Вартість перевезень за опорним планом:");
            return x;
        }


       
        //  МЕТОД ПОТЕНЦІАЛІВ 
        private static int[,] PotentialsMODI(int[,] c, int[,] startPlan)
        {
            int m = c.GetLength(0), n = c.GetLength(1);
            int[,] plan = (int[,])startPlan.Clone();

            // гарантуємо m+n-1 базисних клітинок
            EnsureDegeneracy(GetAllCells(c), plan, m, n);

            int iter = 0;
            while (true)
            {
                iter++;

                //Потенціали u, v 
                int?[] u = new int?[m];
                int?[] v = new int?[n];
                u[0] = 0;
                bool changed;
                do
                {
                    changed = false;
                    for (int i = 0; i < m; i++)
                        for (int j = 0; j < n; j++)
                            if (IsBasic(plan, i, j))
                            {
                                if (u[i].HasValue && !v[j].HasValue)
                                { v[j] = c[i, j] - u[i]; changed = true; }
                                else if (!u[i].HasValue && v[j].HasValue)
                                { u[i] = c[i, j] - v[j]; changed = true; }
                            }
                } while (changed);

                //  Δ = c – u – v (мінімізація)
                int bestI = -1, bestJ = -1, minDelta = 0;
                for (int i = 0; i < m; i++)
                    for (int j = 0; j < n; j++)
                        if (!IsBasic(plan, i, j))
                        {
                            int delta = c[i, j] - (u[i] ?? 0) - (v[j] ?? 0);
                            if (delta < minDelta) { minDelta = delta; bestI = i; bestJ = j; }
                        }

                // Оптимальність?
                if (minDelta >= 0)
                {
                    Console.WriteLine($"\nОптимум досягнено на ітерації {iter} — усі Δ ≥ 0.");
                    PrintPlan(plan);
                    PrintCost(c, plan, "S_opt =");
                    return plan;
                }

                Console.WriteLine($"\nІтерація {iter}. Вибрана клітинка [{bestI + 1},{bestJ + 1}] з Δ = {minDelta}");

                // Побудова циклу 
                var loop = BuildCycle(GetBasicCells(plan), (bestI, bestJ));
                Console.WriteLine("Цикл (+/-): " +
                    string.Join(" → ", loop.Select((p, k) => (k % 2 == 0 ? "+" : "-") + $"[{p.i + 1},{p.j + 1}]")));

                // Знаходимо θ 
                int theta = loop.Where((p, k) => k % 2 == 1).Min(p => plan[p.i, p.j]);
                Console.WriteLine($"λ = {theta}");

                // Оновлюємо план 
                for (int k = 0; k < loop.Count; k++)
                {
                    var (i, j) = loop[k];
                    if (k % 2 == 0)           // знак «+»
                        plan[i, j] = plan[i, j] < 0 ? theta : plan[i, j] + theta;
                    else                      // знак «-»
                        plan[i, j] -= theta;
                }

                // очищаємо нульові базисні
                for (int i = 0; i < m; i++)
                    for (int j = 0; j < n; j++)
                        if (IsBasic(plan, i, j) && plan[i, j] == 0)
                            plan[i, j] = -1;

                // вводимо нову клітинку до базису (вона вже має θ)
                EnsureDegeneracy(GetAllCells(c), plan, m, n);

                Console.WriteLine("Новий план перевезень:");
                PrintPlan(plan);
                PrintCost(c, plan, "Поточна вартість:");
            }
        }


        private static bool IsBasic(int[,] plan, int i, int j) => plan[i, j] >= 0;
        // створює таблицю, де ВСІ клітинки поки небазисні (–1)
        private static int[,] CreateEmptyPlan(int m, int n)
        {
            int[,] p = new int[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    p[i, j] = -1;
            return p;
        }


        // повертає всі клітинки з цінами
        private static List<(int cost, int i, int j)> GetAllCells(int[,] c)
        {
            int m = c.GetLength(0), n = c.GetLength(1);
            var list = new List<(int cost, int i, int j)>();
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    list.Add((c[i, j], i, j));
            list.Sort((a, b) => a.cost.CompareTo(b.cost));
            return list;
        }

        // додає нульові базисні, щоб базис мав m+n‑1 клітинок
        private static void EnsureDegeneracy(List<(int cost, int i, int j)> cells, int[,] plan, int m, int n)
        {
            int basicCount = 0;
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (IsBasic(plan, i, j)) basicCount++;
            foreach (var cell in cells)
            {
                if (basicCount >= m + n - 1) break;
                if (!IsBasic(plan, cell.i, cell.j))
                {
                    plan[cell.i, cell.j] = 0; // робимо базисною з нульовим постачанням
                    basicCount++;
                }
            }
        }

        private static List<(int i, int j)> GetBasicCells(int[,] plan)
        {
            int m = plan.GetLength(0), n = plan.GetLength(1);
            var list = new List<(int i, int j)>();
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (IsBasic(plan, i, j)) list.Add((i, j));
            return list;
        }

        // Пошук замкненого циклу для метолу потенціалів
        private static List<(int i, int j)> BuildCycle(List<(int i, int j)> basis, (int i, int j) enter)
        {
            var nodes = new List<(int i, int j)>(basis) { enter };
            int N = nodes.Count;
            int start = N - 1;   // індекс enter у списку

            var parent = new Dictionary<(int idx, bool rowMove), (int idx, bool rowMove)>();
            var queue = new Queue<(int idx, bool rowMove)>();
            queue.Enqueue((start, true));        // спершу рухаємося по рядку
            queue.Enqueue((start, false));       // або по стовпцю

            while (queue.Count > 0)
            {
                var (cur, byRow) = queue.Dequeue();
                var (ci, cj) = nodes[cur];

                for (int k = 0; k < N; k++)
                {
                    if (k == cur) continue;
                    var (ni, nj) = nodes[k];
                    bool connected = byRow ? ci == ni : cj == nj;
                    if (!connected) continue;

                    var nxt = (k, !byRow);
                    if (parent.ContainsKey(nxt)) continue;   // уже відвідано

                    parent[nxt] = (cur, byRow);
                    if (k == start)         // замкнулися
                    {
                        // реконструюємо шлях
                        var path = new List<(int i, int j)>();
                        var state = (cur, byRow);
                        while (true)
                        {
                            path.Add(nodes[state.Item1]);
                            if (state.Item1 == start) break;
                            state = parent[state];
                        }
                        path.Reverse();
                        return path;
                    }
                    queue.Enqueue(nxt);
                }
            }
            throw new Exception("Цикл не знайдено — перевірте виродженість плану");
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
                    string cell = plan[i, j] < 0 ? "  x"           // –1 ⇒ пусто
                                  : plan[i, j].ToString().PadLeft(3);
                    Console.Write(cell + (j < n - 1 ? " " : ""));
                }
                Console.WriteLine(i == m - 1 ? " )" : "");
            }
            Console.WriteLine();
        }




        private static void PrintCost(int[,] c, int[,] plan, string title)
        {
            int m = plan.GetLength(0), n = plan.GetLength(1);
            int cost = 0;
            var formula = new List<string>();
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (plan[i, j] > 0)
                    {
                        cost += plan[i, j] * c[i, j];
                        formula.Add($"{plan[i, j]} * {c[i, j]}");
                    }
            Console.WriteLine(title);
            Console.WriteLine($"S = {string.Join(" + ", formula)} = {cost}\n");
        }


        private static int[,] ReadMatrix(int rows, int cols, string name)
        {
            Console.WriteLine($"\nВведіть матрицю {name} розміром {rows}×{cols} " +
                              "(елементи через пробіл):");
            int[,] m = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                while (true)
                {
                    Console.Write($"  рядок {i + 1}: ");
                    var parts = Console.ReadLine()!
                                  .Split(new[] { ' ', ',', ';', '\t' },
                                         StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != cols)
                    {
                        Console.WriteLine($"    ❌ Треба саме {cols} чисел, спробуйте ще раз.");
                        continue;
                    }
                    try
                    {
                        for (int j = 0; j < cols; j++) m[i, j] = int.Parse(parts[j]);
                        break;                 // успіх – переходимо до наступного рядка
                    }
                    catch
                    {
                        Console.WriteLine("    ❌ Введено неціле число, спробуйте ще раз.");
                    }
                }
            }
            return m;
        }

        private static int[] ReadVector(int len, string name)
        {
            Console.WriteLine($"\nВведіть вектор {name} ({len} чисел через пробіл):");
            while (true)
            {
                var parts = Console.ReadLine()!
                              .Split(new[] { ' ', ',', ';', '\t' },
                                     StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != len)
                {
                    Console.WriteLine($"    ❌ Треба рівно {len} чисел, спробуйте ще раз.");
                    continue;
                }
                try { return parts.Select(int.Parse).ToArray(); }
                catch
                {
                    Console.WriteLine("    ❌ Є нецілі числа, спробуйте ще раз.");
                }
            }
        }


        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Розв’язувач транспортної задачі ===");

            
            Console.Write("\nКількість пунктів відправлення (m): ");
            int m = int.Parse(Console.ReadLine()!);
            Console.Write("Кількість пунктів призначення  (n): ");
            int n = int.Parse(Console.ReadLine()!);

            
            int[,] cost = ReadMatrix(m, n, "витрат (c_ij)");
            int[] PO = ReadVector(m, "PO (запаси)");
            int[] PN = ReadVector(n, "PN (заявки)");

            
            int sumPO = PO.Sum(), sumPN = PN.Sum();
            if (sumPO != sumPN)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠️  Небалансована задача: ΣPO={sumPO}, ΣPN={sumPN}");
                Console.WriteLine("    Додайте фіктивний рядок або стовпець та заповніть вручну.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("\nВихідні дані:");
            PrintMatrix(cost, "c");
            PrintVector(PO, "PO");
            PrintVector(PN, "PN");

            
            var nwPlan = NorthWestCorner(cost, PO, PN);
            var lcPlan = LeastCost(cost, PO, PN);
            var optPlan = PotentialsMODI(cost, lcPlan);

            Console.WriteLine("Натисніть Enter, щоб завершити …");
            Console.ReadLine();
        }

    }
}




























