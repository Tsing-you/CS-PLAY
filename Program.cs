using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame
{
    // 蛇的方向枚举
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    // 蛋白的身体节点
    public class SnakeSegment
    {
        public int X { get; set; }
        public int Y { get; set; }

        public SnakeSegment(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // 智谱清言API响应模型
    public class ZhipuAIResponse
    {
        public List<Choice> choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string content { get; set; }
    }

    class Program
    {
        // 游戏配置
        static readonly int width = 20;
        static readonly int height = 20;
        static readonly char snakeChar = 'O';
        static readonly char foodChar = '*';
        static readonly char emptyChar = '.';
        static readonly char wallChar = '#';

        // 游戏状态
        static List<SnakeSegment> snake = new List<SnakeSegment>();
        static SnakeSegment food = new SnakeSegment(0, 0);
        static Direction direction = Direction.Right;
        static bool gameOver = false;
        static bool isPaused = false;
        static int score = 0;
        static Random random = new Random();

        // 智谱清言API配置
        static readonly string apiKey = "YOUR_ZHIPU_API_KEY"; // 替换为您的API密钥
        static readonly string apiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

        static async Task Main(string[] args)
        {
            bool playAgain = true;
            
            while (playAgain)
            {
                // 初始化游戏
                InitializeGame();
                
                Console.WriteLine("欢迎来到贪吃蛇游戏！");
                Console.WriteLine("使用WASD或方向键控制蛇的移动");
                Console.WriteLine("按任意键开始游戏...");
                
                // 更安全地等待用户按键
                try
                {
                    Console.ReadKey();
                }
                catch (InvalidOperationException)
                {
                    Console.ReadLine();
                }
                
                // 显示游戏提示
                await ShowGameTip();
                
                // 游戏主循环
                while (!gameOver)
                {
                    if (!isPaused)
                    {
                        HandleInput();
                        Update();
                        Draw();
                        Thread.Sleep(100); // 控制游戏速度
                    }
                    else
                    {
                        await HandleAIConversation();
                        Draw();
                        Thread.Sleep(100); // 暂停时的刷新速度
                    }
                }
                
                // 游戏结束后询问是否重新开始
                playAgain = AskPlayAgain();
            }
            
            Console.Clear();
            Console.WriteLine("感谢游玩！再见！");
            Console.WriteLine("按任意键退出...");
            
            try
            {
                Console.ReadKey();
            }
            catch (InvalidOperationException)
            {
                Console.ReadLine();
            }
        }

        // 初始化游戏
        static void InitializeGame()
        {
            // 初始化蛇的位置（在屏幕中央）
            snake.Clear();
            snake.Add(new SnakeSegment(width / 2, height / 2));
            snake.Add(new SnakeSegment(width / 2 - 1, height / 2));
            snake.Add(new SnakeSegment(width / 2 - 2, height / 2));
            
            // 初始化食物位置
            GenerateFood();
            
            // 重置游戏状态
            direction = Direction.Right;
            gameOver = false;
            isPaused = false;
            score = 0;
        }

        // 生成食物
        static void GenerateFood()
        {
            int x, y;
            bool onSnake;
            
            do
            {
                onSnake = false;
                x = random.Next(0, width);
                y = random.Next(0, height);
                
                // 检查食物是否生成在蛇身上
                foreach (var segment in snake)
                {
                    if (segment.X == x && segment.Y == y)
                    {
                        onSnake = true;
                        break;
                    }
                }
            } while (onSnake);
            
            food.X = x;
            food.Y = y;
        }

        // 处理用户输入
        static void HandleInput()
        {
            // 更安全地检查键盘输入
            if (Console.KeyAvailable)
            {
                ConsoleKey key;
                try
                {
                    key = Console.ReadKey(true).Key;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        if (direction != Direction.Down)
                            direction = Direction.Up;
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        if (direction != Direction.Up)
                            direction = Direction.Down;
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        if (direction != Direction.Right)
                            direction = Direction.Left;
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        if (direction != Direction.Left)
                            direction = Direction.Right;
                        break;
                    case ConsoleKey.R:
                        // 重新开始游戏
                        InitializeGame();
                        break;
                    case ConsoleKey.T:
                        // 获取游戏提示
                        _ = ShowGameTip();
                        break;
                    case ConsoleKey.P:
                        // 暂停/继续游戏
                        isPaused = !isPaused;
                        break;
                }
            }
        }

        // 更新游戏状态
        static void Update()
        {
            // 计算蛇头的新位置
            SnakeSegment head = snake[0];
            SnakeSegment newHead = new SnakeSegment(head.X, head.Y);
            
            switch (direction)
            {
                case Direction.Up:
                    newHead.Y--;
                    break;
                case Direction.Down:
                    newHead.Y++;
                    break;
                case Direction.Left:
                    newHead.X--;
                    break;
                case Direction.Right:
                    newHead.X++;
                    break;
            }
            
            // 检查碰撞边界
            if (newHead.X < 0 || newHead.X >= width || newHead.Y < 0 || newHead.Y >= height)
            {
                gameOver = true;
                return;
            }
            
            // 检查碰撞自己
            foreach (var segment in snake)
            {
                if (segment.X == newHead.X && segment.Y == newHead.Y)
                {
                    gameOver = true;
                    return;
                }
            }
            
            // 将新头部添加到蛇身
            snake.Insert(0, newHead);
            
            // 检查是否吃到食物
            if (newHead.X == food.X && newHead.Y == food.Y)
            {
                // 增加分数
                score += 10;
                // 生成新的食物
                GenerateFood();
            }
            else
            {
                // 移除尾部（如果没有吃到食物）
                snake.RemoveAt(snake.Count - 1);
            }
        }

        // 绘制游戏画面
        static void Draw()
        {
            Console.Clear();
            
            // 绘制顶部边界
            Console.Write(wallChar);
            for (int i = 0; i < width; i++)
                Console.Write(wallChar);
            Console.WriteLine(wallChar);
            
            // 绘制游戏区域
            for (int y = 0; y < height; y++)
            {
                // 左边界
                Console.Write(wallChar);
                
                for (int x = 0; x < width; x++)
                {
                    bool drawn = false;
                    
                    // 绘制蛇身
                    foreach (var segment in snake)
                    {
                        if (segment.X == x && segment.Y == y)
                        {
                            Console.Write(snakeChar);
                            drawn = true;
                            break;
                        }
                    }
                    
                    // 绘制食物
                    if (!drawn && food.X == x && food.Y == y)
                    {
                        Console.Write(foodChar);
                        drawn = true;
                    }
                    
                    // 绘制空地
                    if (!drawn)
                        Console.Write(emptyChar);
                }
                
                // 右边界
                Console.WriteLine(wallChar);
            }
            
            // 绘制底部边界
            Console.Write(wallChar);
            for (int i = 0; i < width; i++)
                Console.Write(wallChar);
            Console.WriteLine(wallChar);
            
            // 显示分数和控制说明
            Console.WriteLine($"分数: {score}");
            if (isPaused)
            {
                Console.WriteLine("游戏已暂停");
                Console.WriteLine("输入问题与AI对话，或按P键继续游戏");
            }
            else
            {
                Console.WriteLine("控制: WASD 或 方向键 | P: 暂停 | R: 重新开始 | T: 获取提示");
            }
        }

        // 处理AI对话
        static async Task HandleAIConversation()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKey key;
                
                try
                {
                    key = Console.ReadKey(true).Key;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                
                if (key == ConsoleKey.P)
                {
                    // 继续游戏
                    isPaused = false;
                    return;
                }
                else
                {
                    // 进入AI对话模式
                    Console.Clear();
                    Console.WriteLine("=== AI 对话模式 ===");
                    Console.WriteLine("请输入您的问题 (按P返回游戏):");
                    string userQuestion = Console.ReadLine();
                    
                    if (!string.IsNullOrWhiteSpace(userQuestion))
                    {
                        if (userQuestion.ToLower() == "p")
                        {
                            isPaused = false;
                        }
                        else
                        {
                            await GetAIResponse(userQuestion);
                            Console.WriteLine("\n按任意键返回游戏...");
                            try
                            {
                                Console.ReadKey();
                            }
                            catch (InvalidOperationException)
                            {
                                Console.ReadLine();
                            }
                        }
                    }
                }
            }
        }

        // 获取AI响应
        static async Task GetAIResponse(string question)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var requestBody = new
                    {
                        model = "glm-4",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = question
                            }
                        },
                        stream = false
                    };

                    string json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var aiResponse = JsonSerializer.Deserialize<ZhipuAIResponse>(responseString);
                        if (aiResponse?.choices?.Count > 0)
                        {
                            Console.Clear();
                            Console.WriteLine("🤖 AI 回答:");
                            Console.WriteLine(aiResponse.choices[0].message.content);
                        }
                    }
                    else
                    {
                        Console.WriteLine("AI服务暂时不可用，请稍后再试。");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("与AI通信时发生错误: " + ex.Message);
            }
        }

        // 调用智谱清言API获取游戏提示
        static async Task ShowGameTip()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var requestBody = new
                    {
                        model = "glm-4",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = "请提供一个贪吃蛇游戏的小技巧或策略，保持简短有趣。"
                            }
                        },
                        stream = false
                    };

                    string json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var aiResponse = JsonSerializer.Deserialize<ZhipuAIResponse>(responseString);
                        if (aiResponse?.choices?.Count > 0)
                        {
                            Console.Clear();
                            Console.WriteLine("💡 智谱AI提示:");
                            Console.WriteLine(aiResponse.choices[0].message.content);
                            Console.WriteLine("\n按任意键继续游戏...");
                            
                            try
                            {
                                Console.ReadKey();
                            }
                            catch (InvalidOperationException)
                            {
                                Console.ReadLine();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果API调用失败，显示本地提示
                Console.Clear();
                Console.WriteLine("💡 游戏提示:");
                Console.WriteLine("1. 不要撞到墙壁或自己的身体");
                Console.WriteLine("2. 尽量让蛇身保持直线移动");
                Console.WriteLine("3. 预判食物位置，提前调整方向");
                Console.WriteLine("\n按任意键继续游戏...");
                
                try
                {
                    Console.ReadKey();
                }
                catch (InvalidOperationException)
                {
                    Console.ReadLine();
                }
            }
        }

        // 询问是否重新开始游戏
        static bool AskPlayAgain()
        {
            Console.Clear();
            Console.WriteLine($"游戏结束！最终得分: {score}");
            Console.WriteLine("是否想要再玩一次？(Y/N)");
            
            try
            {
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine(); // 换行
                
                if (key == ConsoleKey.Y || key == ConsoleKey.Enter)
                {
                    return true;
                }
                else if (key == ConsoleKey.N)
                {
                    return false;
                }
                else
                {
                    // 默认情况下，如果按其他键则询问是否退出
                    Console.WriteLine("无效输入。按Y重新开始，按N退出。");
                    return AskPlayAgain(); // 递归调用直到获得有效输入
                }
            }
            catch (InvalidOperationException)
            {
                // 如果无法读取按键，则读取一行
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "y" || input.ToLower() == "yes")
                {
                    return true;
                }
                else if (input.ToLower() == "n" || input.ToLower() == "no")
                {
                    return false;
                }
                else
                {
                    Console.WriteLine("无效输入。请输入Y(是)或N(否)。");
                    return AskPlayAgain(); // 递归调用直到获得有效输入
                }
            }
        }
    }
}