using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Serialization;
using WpfSokoban.Messages;
using WpfSokoban.Models;
using System.Configuration;
using System.Collections.Specialized;

namespace WpfSokoban.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        /// Модель левела
        [ObservableProperty]
        private Level level = new();

        //MessageBox для правил
        public string y;

        [RelayCommand]
        public void Rules()
        {
            var appSettings = ConfigurationManager.AppSettings;
            // Установка значений задержки из настроек приложения
            y = appSettings["y"];
            MessageBox.Show(y);
        }

        public MainWindowViewModel() 
        {
            Level.LoadLevel();

            var properties = typeof(Resource).GetProperties();

            WeakReferenceMessenger.Default.Register<NotifyUndoAvailabilityMessage>(this, (r, m) =>
            {
                UndoCommand.NotifyCanExecuteChanged();
            });
        }

        //Метод сохранения игры
        [RelayCommand]
        public void SaveGame()
        {
            var level = Level.CurrentLevel;
            var steps = Level.StepCount;
            var x = Level.Hero.X;
            var y = Level.Hero.Y;
            var fileName = "saveGame.txt";

            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.Write(level.ToString());
                sw.Write("\n");
                sw.Write(steps);
                sw.Write("\n");
                sw.Write(x);
                sw.Write(" ");
                sw.Write(y);
                sw.Write("\n");
                foreach (MovableObject crate in Level.Crates)
                {
                    x = crate.X;
                    y = crate.Y;
                    {
                        sw.Write(x);
                        sw.Write(" ");
                        sw.Write(y);
                        sw.Write("\n");
                    }
                }
            }
        }

        //Загрузка игры
        [RelayCommand]
        public void LoadGame()
        {
            
            var inputFileName = "saveGame.txt";
            string fileContents;
            using (StreamReader sr = File.OpenText(inputFileName))
            {
                fileContents = sr.ReadLine();

                //Еще одни костыли )
                Level.LoadLevelFile(Level.GetLevel(int.Parse(fileContents)));
                Level.Map.Add(new WpfSokoban.Models.Block(BlockType.Space, 10, 10));
                Level.Crates.Add(new MovableObject(MovableObjectType.Crate, 10, 10));


                Level.CurrentLevel = int.Parse(fileContents);
                fileContents = sr.ReadLine();
                Level.StepCount = int.Parse(fileContents);
                fileContents = sr.ReadLine();
                string[] coords = fileContents.Split(" ");
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                Level.Map.Add(new WpfSokoban.Models.Block(BlockType.Space, x, y));
                Level.Hero = new MovableObject(MovableObjectType.Hero, x, y);
                

                while (!sr.EndOfStream)
                {
                    fileContents = sr.ReadLine();
                    coords = fileContents.Split(" ");
                    x = int.Parse(coords[0]);
                    y = int.Parse(coords[1]);
                    Level.Map.Add(new WpfSokoban.Models.Block(BlockType.Space, x, y));
                    Level.Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                }
            }
            //Костыли)))))))
            Level.IsWinning = false;
            Level.Crates.RemoveAt(0);
        }

        /// Обрабатывем событие нажатия клавиши в окне
        [RelayCommand]
        private void WindowKeyDown(KeyEventArgs e)
        {
            if (Level.IsWinning)
            {
                if (e.Key == Key.Enter && Level.HasMoreLevels)
                    NextLevelCommand.Execute(null);
                else
                    return;
            }

            var x = Level.Hero.X;
            var y = Level.Hero.Y;

            (int x, int y) offset;

            // Каждая клавиша - свое смещение по координатам
            switch (e.Key)
            {
                case Key.Up:
                    offset = (0, -1);
                    break;
                case Key.Down:
                    offset = (0, 1);
                    break;
                case Key.Left:
                    offset = (-1, 0);
                    break;
                case Key.Right:
                    offset = (1, 0);
                    break;
                default:
                    return;
            }

            x += offset.x;
            y += offset.y;

            // Если игрок упирается в стену
            bool canHeroMove = !Level.HasWallAt(x, y);

            if (!canHeroMove)
                return;

            // ИГрок упирается в ящик
            var hitCrate = Level.HasCrateAt(x, y);

            if (hitCrate is not null)
            {
                // Проверка, что ящик может двигаться
                var cx = hitCrate.X + offset.x;
                var cy = hitCrate.Y + offset.y;

                bool canCrateMove = !Level.HasWallAt(cx, cy) && Level.HasCrateAt(cx, cy) == null;

                if (!canCrateMove)
                    return;

                // Движение ящика
                hitCrate.Move(offset);
                hitCrate.CheckOnGoal(Level);

                Level.History.Push((hitCrate, offset));
            }

            // Если движение возможно

            Level.Hero.Move(offset);
            Level.History.Push((Level.Hero, offset));

            Level.StepCount++;
        }

        /// Загрузка следующего уровня
        [RelayCommand(CanExecute = nameof(CanNextLevelExecute))]
        private void NextLevel()
        {
            Level.TryLoadNextLevel();
            NextLevelCommand.NotifyCanExecuteChanged();
        }

        private bool CanNextLevelExecute() => Level.HasMoreLevels;

        /// Рестарт левела
        [RelayCommand]
        private void Restart() => Level.LoadLevel();

        /// Откат одного шага
        [RelayCommand(CanExecute = nameof(CanUndoExecute))]
        private void Undo()
        {
            Level.Undo();
            UndoCommand.NotifyCanExecuteChanged();
        }

        private bool CanUndoExecute() => Level.History.Count > 0;
    }
           
}
