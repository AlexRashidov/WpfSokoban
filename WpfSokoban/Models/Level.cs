using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using WpfSokoban.Messages;

namespace WpfSokoban.Models
{
    public partial class Level : ObservableObject
    {

        /// Размер сетки
        public static int GridSize = 50;

        public static int LevelCount;

        //директива препроцессора, чтобы скрывать некоторые части кода 
        #region Observable Properties

        /// Показывает текущий индекс уровня 
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMoreLevels))]
        private int currentLevel = 1;

        /// Метка, которая показывает есть ли еще уровни для прохождения или нет
        public bool HasMoreLevels => CurrentLevel < LevelCount;

        /// По сути катра данного уровня
        [ObservableProperty]
        private ObservableCollection<Block> map = new();

        /// Объект, управляемый игроком
        [ObservableProperty]
        private MovableObject hero;

        /// Все ящики на данном уровнеи
        [ObservableProperty]
        private ObservableCollection<MovableObject> crates = new();

        ///Стек для записи действий игрока, чтобы была возможность вернуть игрока на ход назад
        [ObservableProperty]
        private Stack<(MovableObject obj, (int, int) offset)> history = new();

        //Ширина и Высота
        [ObservableProperty]
        private int width;

        [ObservableProperty]
        private int height;

        //Кол - во шагов игрока
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWinning), nameof(History))]
        private int stepCount = 0;

        #endregion

        //Метод для получения кол - ва уровней
        public Level()
        {
            GetLevelCount();
        }

        partial void OnStepCountChanged(int value)
        {
            SendNotifyUndoAvailabilityMessage();
        }

        public void LoadLevel(string text)
        {
            Init();
            (Width, Height) = ParseLevelString(text);

            Width = GridSize * (Width + 1);
            Height = GridSize * (Height + 1);

            OnPropertyChanged(nameof(IsWinning));

            SendNotifyUndoAvailabilityMessage();
        }
        //Из файла парсим
        public void LoadLevelFile(string text)
        {
            Init();
            (Width, Height) = ParseLevelStringLoad(text);

            Width = GridSize * (Width + 1);
            Height = GridSize * (Height + 1);

            OnPropertyChanged(nameof(IsWinning));

            SendNotifyUndoAvailabilityMessage();
        }


        public void LoadLevel(int? level = null)
        {
            if (level == null)
                level = CurrentLevel;
            LoadLevel(GetLevel(level.Value));
        }

        /// Метод, который пытается загрузить следующий лвл

        public bool TryLoadNextLevel()
        {
            try
            {
                var str = GetLevel(CurrentLevel + 1);
                LoadLevel(str);
                CurrentLevel++;
                return true;
            }
            catch (IndexOutOfRangeException) //ловля ошибки 
            {
                return false;
            }
        }


        /// Инициализация объектов, их чистка
        private void Init()
        {
            Map.Clear();
            Crates.Clear();
            History.Clear();

            StepCount = 0;
        }

        /// Получение количества уровней, просто просмотрев ресурсы
        private void GetLevelCount()
        {
            int level = 1;

            while (true)
            {
                var prop = typeof(Resource).GetProperty($"Level{level}", BindingFlags.Static | BindingFlags.NonPublic);
                if (prop != null)
                    level++;
                else
                    break;
            }

            LevelCount = level - 1;
        }

        private void SendNotifyUndoAvailabilityMessage()
        {
            WeakReferenceMessenger.Default.Send(new NotifyUndoAvailabilityMessage(null));
        }

        /// Конвертирует int в string в ресурсы
        public string GetLevel(int level)
        {
            if (level < 1 || level > LevelCount) throw new IndexOutOfRangeException(); //Ловим ошибку 

            var prop = typeof(Resource).GetProperty($"Level{level}", BindingFlags.Static | BindingFlags.NonPublic);
            return prop!.GetValue(null) as string;
        }

        private (int width, int height) ParseLevelString(string text)
        {
            int width = 0, height = 0;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                for (int j = 0; j < line.Length; j++)
                {
                    var ch = line[j];

                    int x = j, y = i;

                    // Получение размера Grid для Canvas
                    if (x > width)
                        width = x;
                    if (y > height)
                        height = y;

                    /**
                     * #: Стенка
                     * @: Герой
                     * _: Пустое пространство
                     * .: Цель
                     * $: Ящик
                     * *: Ящик на цели
                     * +: Герой на цели
                     */

                    switch (ch)
                    {
                        case '#':
                            Map.Add(new Block(BlockType.Wall, x, y));
                            break;
                        case '.':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            break;
                        case '@':
                            Map.Add(new Block(BlockType.Space, x, y));
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                            break;
                        case '$':
                            Map.Add(new Block(BlockType.Space, x, y));
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                            break;
                        case '*':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                            break;
                        case '+':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                            break;
                    }
                }
            }

            return (width, height);
        }
        //Парс лвл load
        private (int width, int height) ParseLevelStringLoad(string text)
        {
            int width = 0, height = 0;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                for (int j = 0; j < line.Length; j++)
                {
                    var ch = line[j];

                    int x = j, y = i;

                    // Получение размера Grid для Canvas
                    if (x > width)
                        width = x;
                    if (y > height)
                        height = y;

                    /**
                     * #: Стенка
                     * @: Герой
                     * _: Пустое пространство
                     * .: Цель
                     * $: Ящик
                     * *: Ящик на цели
                     * +: Герой на цели
                     */

                    switch (ch)
                    {
                        case '#':
                            Map.Add(new Block(BlockType.Wall, x, y));
                            break;
                        case '.':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            break;
                        case '*':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                            break;
                        case '+':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                            break;
                    }
                }
            }

            return (width, height);
        }

        //метод проверяет, есть ли на карте объект типа Wall с заданными координатами (x, y) и возвращает true, если он найден, и false в противном случае.
        public bool HasWallAt(int x, int y)
        {
            return Map.Where(b => b.Type == BlockType.Wall).Any(block => block.X == x && block.Y == y);
        }
        //Проверка на победу
        public bool IsWinning
        {
            get
            {
                return Crates.All(crate => crate.IsOnGoal);
            }
            set
            {
                ;
            }
        }

        //метод проверяет, есть ли на карте объект  с указанными координатами (x, y) и возвращает этот объект, если он найден, или null, если такого объекта нет
        public MovableObject HasCrateAt(int x, int y)
        {
            return Crates.FirstOrDefault(crate => crate.X == x && crate.Y == y);
        }


        /// Возвращение на один шаг
        public void Undo()
        {
            Debug.Assert(History.Count > 0);

            //Возрат только героя
            var (hero, offset) = History.Pop();
            StepCount -= 1;
            hero.Reverse(offset);

            // Если еще двигался ящик, то возврат ящик на предыдущее место тоже
            if (History.TryPeek(out var move) && move.obj.Type == MovableObjectType.Crate)
            {
                History.Pop();
                move.obj.Reverse(move.offset);
                move.obj.CheckOnGoal(this);
            }

            OnPropertyChanged(nameof(History));
        }
    }
}
