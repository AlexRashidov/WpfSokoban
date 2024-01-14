using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace WpfSokoban.Models
{
  
    public partial class MovableObject : ObservableObject
    {
        //Получаю тип объекта и его координаты
        public MovableObject(MovableObjectType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }
        
        [ObservableProperty]
        private MovableObjectType type;

        // Координата X
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualX))]
        private int x;


        // Координата Y
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualY))]
        private int y;

        /// Координата X для Canvas
        public int ActualX => X * Level.GridSize;

        /// Координата Y для Canvas
        public int ActualY => Y * Level.GridSize;

        /// <summary>
        /// Ящика на метке(изначально нет)
        /// </summary>
        [ObservableProperty]
        private bool isOnGoal = false;
        
        //Метод для движения(просто прибавляет координаты по x и y)
        private void Move(int x, int y)
        {
            X += x;
            Y += y;
        }

        /// Метод для движения(сам герой и/или ящик начинает двигаться)
        public void Move((int x, int y) offset)
        {
            Move(offset.x, offset.y);
        }

        /// Метод для хода "назад"(типо возвращение на 1 ход назад)
        public void Reverse((int x, int y) offset)
        {
            Move(-offset.x, -offset.y);
        }

        /// Прверка стоит ли ящик на конечной точке(в нашем случае на Crosshairs)
        /// Этот код проверяет, находится ли объект типа MovableObjectType на блоке с целью на карте уровня и соответственно устанавливает значение IsOnGoal в true или false в зависимости от этого.
        public void CheckOnGoal(Level level)
        {
            // Для героя нет смысла это проверять
            if (Type == MovableObjectType.Hero)
                return;

            if (level.Map.Where(b => b.Type == BlockType.Goal).Any(block => block.X == X && block.Y == Y))
            {
                IsOnGoal = true;
                return;
            }
            IsOnGoal = false;
        }
    }
}
