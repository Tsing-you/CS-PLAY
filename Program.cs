using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

// 蛇的身体节点
public class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// 智谱AI API请求模型
public class ZhipuMessage
{
    public string role { get; set; }
    public string content { get; set; }
}

public class ZhipuRequest
{
    public string model { get; set; } = "glm-4.5-flash";
    public List<ZhipuMessage> messages { get; set; }
}

public class ZhipuChoice
{
    public ZhipuMessage message { get; set; }
}

public class ZhipuResponse
{
    public List<ZhipuChoice> choices { get; set; }
}

class SnakeGame
{
    // 游戏区域大小
    const int Width = 20;
    const int Height = 20;

    // 游戏元素
    List<Point> snake;
    Point food;
    Point direction;
    bool gameOver;
    int score;

    // 智谱AI API密钥 (请替换为您自己的API密钥)
    private const string API_KEY = "62aca7a83e7a40308d2f4f51516884bc.J91FkaxCor4k3sDk";
    private static readonly HttpClient client = new HttpClient();

    static void Main(string[] args)
    {
        Console.WriteLine("欢迎来到贪吃蛇游戏！");
        Console.WriteLine("使用WASD控制方向，按任意键开始游戏...");
        Console.ReadKey();

        var game = new SnakeGame();
        game.StartGame();
    }

    public void StartGame()
    {
        InitializeGame();
        GameLoop();
        EndGame();
    }

    // 初始化游戏
    private void InitializeGame()
    {
        // 初始化蛇的位置 (在屏幕中央)
        snake = new List<Point>
        {
            new Point(Width / 2, Height / 2),
            new Point(Width / 2 - 1, Height / 2),
            new Point(Width / 2 - 2, Height / 2)
        };

        // 初始移动方向向右
        direction = new Point(1, 0);

        // 生成食物
        GenerateFood();

        gameOver = false;
        score = 0;
    }

    // 主游戏循环
    private void GameLoop()
    {
        while (!gameOver)
        {
            // 处理用户输入
            HandleInput();

            // 更新游戏状态
            Update();

            // 绘制游戏画面
            Draw();

            // 控制游戏速度
            System.Threading.Thread.Sleep(200);
        }
    }

    // 处理键盘输入
    private void HandleInput()
    {
        if (!Console.KeyAvailable) return;

        var key = Console.ReadKey(true).Key;
        switch (key)
        {
            case ConsoleKey.W:
                if (direction.Y == 0) direction = new Point(0, -1); // 上
                break;
            case ConsoleKey.S:
                if (direction.Y == 0) direction = new Point(0, 1);  // 下
                break;
            case ConsoleKey.A:
                if (direction.X == 0) direction = new Point(-1, 0); // 左
                break;
            case ConsoleKey.D:
                if (direction.X == 0) direction = new Point(1, 0);  // 右
                break;
        }
    }

    // 更新游戏状态
    private void Update()
    {
        // 计算蛇头的新位置
        var head = snake[0];
        var newHead = new Point(head.X + direction.X, head.Y + direction.Y);

        // 检查是否撞墙
        if (newHead.X < 0 || newHead.X >= Width || newHead.Y < 0 || newHead.Y >= Height)
        {
            gameOver = true;
            return;
        }

        // 检查是否撞到自己
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
            // 生成新食物
            GenerateFood();
        }
        else
        {
            // 移除尾部 (如果没有吃到食物)
            snake.RemoveAt(snake.Count - 1);
        }
    }

    // 生成食物
    private void GenerateFood()
    {
        var random = new Random();
        Point newFood;

        do
        {
            newFood = new Point(
                random.Next(0, Width),
                random.Next(0, Height)
            );
        }
        while (snake.Exists(segment => segment.X == newFood.X && segment.Y == newFood.Y));

        food = newFood;
    }

    // 绘制游戏画面
    private void Draw()
    {
        // 清屏
        Console.Clear();

        // 创建游戏画布
        char[,] grid = new char[Height, Width];

        // 初始化为空格
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                grid[y, x] = ' ';
            }
        }

        // 绘制蛇身
        for (int i = 0; i < snake.Count; i++)
        {
            var segment = snake[i];
            // 蛇头用特殊符号
            grid[segment.Y, segment.X] = (i == 0) ? 'O' : 'o';
        }

        // 绘制食物
        grid[food.Y, food.X] = '@';

        // 绘制边界和游戏画面
        Console.WriteLine("+" + new string('-', Width) + "+");
        for (int y = 0; y < Height; y++)
        {
            Console.Write("|");
            for (int x = 0; x < Width; x++)
            {
                Console.Write(grid[y, x]);
            }
            Console.WriteLine("|");
        }
        Console.WriteLine("+" + new string('-', Width) + "+");

        // 显示分数
        Console.WriteLine($"分数: {score}  使用WASD控制方向");
    }

    // 游戏结束处理
    private void EndGame()
    {
        Console.Clear();
        Console.WriteLine("游戏结束!");
        Console.WriteLine($"最终得分: {score}");
        Console.WriteLine("正在连接智谱AI获取游戏技巧提示...");

        // 调用智谱AI API获取游戏技巧
        GetAIGameTip().Wait();

        Console.WriteLine("\n按任意键退出游戏...");
        Console.ReadKey();
    }

    // 调用智谱AI API获取游戏技巧
    private async Task GetAIGameTip()
    {
        try
        {
            var messages = new List<ZhipuMessage>
            {
                new ZhipuMessage { role = "user", content = "你是一个游戏专家，玩家刚刚玩完贪吃蛇游戏，每吃一个得10分，得了" + score + "分。请根据他的分数给他一些游戏技巧建议，并鼓励他继续努力。要求极其简短，以下所有回答都在100字以内" },
            };

            var request = new ZhipuRequest
            {
                messages = messages
            };

            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 添加认证头
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {"62aca7a83e7a40308d2f4f51516884bc.J91FkaxCor4k3sDk"}");

            var response = await client.PostAsync("https://open.bigmodel.cn/api/paas/v4/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<ZhipuResponse>(responseString);
                if (result.choices != null && result.choices.Count > 0)
                {
                    Console.WriteLine("\n🤖 AI游戏技巧提示:");
                    Console.WriteLine(result.choices[0].message.content);
                }
            }
            else
            {
                Console.WriteLine($"AI服务暂时不可用 (错误: {response.StatusCode})");
                Console.WriteLine("游戏技巧: 多练习可以提高反应速度，尝试规划蛇的路径以避免撞到自己。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接AI服务时出错: {ex.Message}");
            Console.WriteLine("游戏技巧: 保持冷静，预判蛇的移动轨迹，合理利用边界转向。");
        }
    }
}