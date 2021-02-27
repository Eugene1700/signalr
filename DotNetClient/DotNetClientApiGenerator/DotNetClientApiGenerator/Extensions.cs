// //////////////////////////////////////////////////////////////////////////////
// //
// // Workfile: Extensions.cs
// // Created by: ruzakov @ 16-11-2011
// // Copyright © 2011, Prosoft-E. All rights reserved.
// //
// // Description
// //
// //----------------------------------------------------------------------------
// // $Id: $
// //----------------------------------------------------------------------------
// //////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    ///     Полезные расширения
    /// </summary>
    public static class Extensions
    {
        private static Func< DateTime, DateTime > _converter;

        /// <summary>
        ///     Элемент встречается во множестве значений
        /// </summary>
        /// <typeparam name="T"> Тип элемента </typeparam>
        /// <param name="value"> Элемента </param>
        /// <param name="items"> Множество значений </param>
        /// <returns> true, если элемент встречается во множестве значений </returns>
        public static bool In< T >( this T value, params T[] items )
        {
            return items.Contains( value );
        }

        /// <summary>
        ///     ForEach для IEnumerable
        /// </summary>
        /// <typeparam name="T"> Тип элемента </typeparam>
        /// <param name="collection"> Коллекция </param>
        /// <param name="action"> Действие </param>
        public static IEnumerable<T> ForEach< T >( this IEnumerable< T > collection, Action< T > action )
        {
            foreach( var c in collection )
            {
                action( c );
                yield return c;
            }
        }

        /// <summary>
        ///     Инициализация конвертера из вермени приложения во время сервера БД
        /// </summary>
        /// <param name="converter"> Функция конвертации </param>
        public static void InitDateTimeConverter( Func< DateTime, DateTime > converter )
        {
            _converter = converter;
        }

        /// <summary>
        ///     Конвертирует во время БД
        /// </summary>
        /// <param name="dateTime"> Входное время (может быть и локальное, и UTC) </param>
        /// <returns> Время, переведнное в часовой пояс БД </returns>
        public static DateTime ToDbTime( this DateTime dateTime )
        {
            if( _converter != null )
            {
                return _converter( dateTime );
            }
            return dateTime;
        }

        /// <summary>
        ///     Обнуляет миллисекунды
        /// </summary>
        /// <param name="dateTime"> Входное время </param>
        /// <returns> Время с обнуленными миллисекундами </returns>
        public static DateTime RoundToSeconds( this DateTime dateTime )
        {
            return dateTime.AddTicks( -( dateTime.Ticks % TimeSpan.TicksPerSecond ) );
        }

        /// <summary>
        /// начало месяца
        /// </summary>
        /// <param name="dateTime">некая дата в месяце</param>
        /// <returns>дату-время начала календ. месяца, в который попадает дата</returns>
        public static DateTime BeginningOfMonth( this DateTime dateTime )
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// конец месяца (в нашем понимании - а именно, начало следующего месяца)
        /// </summary>
        /// <param name="dateTime">некая дата в месяце</param>
        /// <returns>дату-время начала след. календ. месяца, в который попадает дата</returns>
        public static DateTime EndOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths( 1 );
        }

        /// <summary>
        /// начало недели
        /// </summary>
        /// <param name="dateTime">некая дата в неделе</param>
        /// <returns>дату-время начала календ. недели, в которую попадает дата</returns>
        public static DateTime BeginningOfWeek(this DateTime dateTime)
        {
            var delta = ( 6 + ( int )dateTime.DayOfWeek ) % 7;
            return dateTime.AddDays( -delta ).Date;
        }

        /// <summary>
        /// начало дня
        /// </summary>
        /// <param name="dateTime">некая дата-время в дне</param>
        /// <returns>дату-время начала дня, в которую попадает дата</returns>
        public static DateTime BeginningOfDay(this DateTime dateTime)
        {
            return new DateTime( dateTime.Year, dateTime.Month, dateTime.Day );
        }

        /// <summary>
        /// конец дня - начало следующего дня
        /// </summary>
        /// <param name="dateTime">некая дата-время в дне</param>
        /// <returns>дату-время начала следующего дня, в которую попадает дата</returns>
        public static DateTime EndOfDay(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day).AddDays( 1 );
        }

        /// <summary>
        /// конец недели (в нашем понимании - а именно, начало следующей недели)
        /// </summary>
        /// <param name="dateTime">некая дата в неделе</param>
        /// <returns>дату-время начала след. календ. недели, в которую попадает дата</returns>
        public static DateTime EndOfWeek(this DateTime dateTime)
        {
            return dateTime.BeginningOfWeek().AddDays( 7 );
        }

        /// <summary>
        /// начало года
        /// </summary>
        /// <param name="dateTime">некая дата в году</param>
        /// <returns>дату-время начала календ. года, в который попадает дата</returns>
        public static DateTime BeginningOfYear(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 1, 1);
        }

        /// <summary>
        /// начало года
        /// </summary>
        /// <param name="dateTime">некая дата в году</param>
        /// <returns>дату-время начала календ. года, в который попадает дата</returns>
        public static DateTimeOffset BeginningOfYear(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset( dateTime.Year, 1, 1, 0, 0, 0, dateTime.Offset );
        }

        /// <summary>
        /// конец года
        /// (в нашем понимании - а именно, начало следующего)
        /// </summary>
        /// <param name="dateTime">некая дата в году</param>
        /// <returns>дату-время начала след. календ. года, в который попадает дата</returns>
        public static DateTime EndOfYear(this DateTime dateTime)
        {
            return new DateTime( dateTime.Year + 1, 1, 1 );
        }

        /// <summary>
        /// конец года
        /// (в нашем понимании - а именно, начало следующего)
        /// </summary>
        /// <param name="dateTime">некая дата в году</param>
        /// <returns>дату-время начала след. календ. года, в который попадает дата</returns>
        public static DateTimeOffset EndOfYear(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset( dateTime.Year + 1, 1, 1, 0, 0, 0, dateTime.Offset );
        }

        /// <summary>
        ///     Округляет до минут вниз
        /// </summary>
        /// <param name="dateTime"> Входное время </param>
        /// <returns> Время с обнуленными секундами и миллисекундами </returns>
        public static DateTime RoundToMinutes(this DateTime dateTime)
        {
            return dateTime.AddTicks( -( dateTime.Ticks % TimeSpan.TicksPerMinute ) );
        }

        /// <summary>
        /// округлить до ближайшего получаса (сейчас - возможно в будущем)
        /// </summary>
        /// <param name="dateTime">входное время</param>
        /// <returns>время округленное до получаса</returns>
        public static DateTime RoundToHalfHour( this DateTime dateTime )
        {
            var possibleDates = new DateTime[]
            {
                new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0), 
                new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 30, 0), 
                new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0).AddHours( 1 ) 
            };
            var minDist = double.MaxValue;
            var minIndex = -1;
            for (int i = 0; i < possibleDates.Length; i++)
            {
                var delta = Math.Abs((possibleDates[i] - dateTime).TotalSeconds);
                if (delta < minDist)
                {
                    minDist = delta;
                    minIndex = i;
                }
            }
            return possibleDates[minIndex];
        }

        /// <summary>
        /// Округляет вниз до заданного значения
        /// </summary>
        /// <param name="date">Входное время</param>
        /// <param name="roundTicks">Кол-во тиков, до которого надо округлить</param>
        /// <returns>Округленное время</returns>
        public static DateTime RoundTo(this DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - (date.Ticks % roundTicks));
        }

        /// <summary>
        /// Окружить строку чем-то, если он еще не окружена этим (напр., заключить в кавычки)
        /// </summary>
        /// <param name="baseString">обрабатываемая строка</param>
        /// <param name="encloseWith">то, во что ее заключаем/чем ее окружаем</param>
        /// <returns>строку с добавленными в начале и в конце (если их еще не было) encloseWith</returns>
        public static string Enclose(this string baseString, string encloseWith)
        {
            if( baseString == null )
            {
                throw new ArgumentNullException( "baseString" );
            }

            if( encloseWith == null )
            {
                throw new ArgumentNullException( "encloseWith" );
            }

            var res = baseString;
            if (res.IndexOf(encloseWith, StringComparison.OrdinalIgnoreCase) != 0)
            {
                res = encloseWith + baseString;
            }
            if (!string.Equals( res.Substring( res.Length-encloseWith.Length ), encloseWith, StringComparison.OrdinalIgnoreCase))
            {
                res = res + encloseWith;
            }
            return res;
        }

        /// <summary>
        /// Снять окружение со строки чем-то (напр., убрать кавычки)
        /// </summary>
        /// <param name="baseString">обрабатываемая строка</param>
        /// <param name="encloseWith">то, во что она может быть заключена/чем может быть окружена</param>
        /// <returns>строку с убранными в начале и конце (если были) encloseWith</returns>
        public static string Unenclose(this string baseString, string encloseWith)
        {
            if( baseString == null )
            {
                throw new ArgumentNullException( "baseString" );
            }

            if( encloseWith == null )
            {
                throw new ArgumentNullException( "encloseWith" );
            }

            if( baseString.IndexOf( encloseWith, StringComparison.Ordinal ) == 0 )
            {
                return baseString.Substring(encloseWith.Length, baseString.Length - (encloseWith.Length * 2));
            }
            return baseString;
        }

        /// <summary>
        /// Преобразует интервал в user-friendly вид в зависимости от продолжительности интервала
        /// </summary>
        /// <param name="timeSpan">интервал</param>
        /// <returns>user-friendly строка</returns>
        public static string SmartFormat(this TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
            {
                return "{0} дней {1} час".FormatInvariant( timeSpan.Days, timeSpan.Hours);
            }
            if (timeSpan.Hours > 0)
            {
                return "{0} час {1} мин".FormatInvariant( timeSpan.Hours, timeSpan.Minutes );
            }
            if (timeSpan.Minutes > 0)
            {
                return "{0} мин {1} сек".FormatInvariant( timeSpan.Minutes, timeSpan.Seconds );
            }
            return "{0} сек {1} миллисек".FormatInvariant( timeSpan.Seconds, timeSpan.Milliseconds );
        }

        /*private static TimeSpan _oneSecond = TimeSpan.FromMilliseconds(1000);
        private static TimeSpan _maxTime = new TimeSpan(0, 23, 59, 59, 500);*/
        
        /// <summary>
        /// Преобразует число в строку с использованием инвариантной культуры
        /// </summary>
        /// <param name="value">Число</param>
        /// <returns>Строка</returns>
        public static string ToStringInvariant(this int value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        private static readonly DateTime UnixBeginningOfTimes = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
        /// <summary>
        /// преобразовать в Unix-подобную метку времени (число мс с 1970.1.1)
        /// ничего не делает по поводу ЧС, часового пояса
        /// </summary>
        /// <param name="value">дата-время</param>
        /// <returns>количество мс (не секунд) между  1970.1.1 и данной датой</returns>
        public static long ToUnixTimestamp( this DateTime value )
        {
            return ( long )( value - UnixBeginningOfTimes ).TotalMilliseconds;
        }

        /// <summary>
        /// преобразовать из Unix-подобной метки времени (число мс с 1970.1.1) в дату-время
        /// </summary>
        /// <param name="stamp">Unix-метка времени (но мс а не с)</param>
        /// <returns>дата-время</returns>
        public static DateTime UnixTimestampToDateTime( this long stamp )
        {
            return UnixBeginningOfTimes.AddMilliseconds( stamp );
        }

        /// <summary>
        /// преобразовать в Unix-метку времени (число мс с 1970.1.1)
        /// ничего не делает по поводу ЧС, часового пояса
        /// </summary>
        /// <param name="value">дата-время</param>
        /// <returns>количество мс между  1970.1.1 и данной датой</returns>
        public static long ToUnixTimestamp(this DateTime? value)
        {
            return value == null ? default(long) : ( long )( value.Value - UnixBeginningOfTimes ).TotalMilliseconds;
        }

        /// <summary>
        /// Преобразует число в строку с использованием инвариантной культуры
        /// </summary>
        /// <param name="value">Число</param>
        /// <returns>Строка</returns>
        public static string ToStringInvariant( this short value )
        {
            return value.ToString( NumberFormatInfo.InvariantInfo );
        }

        /// <summary>
        /// Преобразует число в строку с использованием инвариантной культуры
        /// </summary>
        /// <param name="value">Число</param>
        /// <returns>Строка</returns>
        public static string ToStringInvariant(this long value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Преобразует число в строку с использованием инвариантной культуры
        /// </summary>
        /// <param name="value">Число</param>
        /// <returns>Строка</returns>
        public static string ToStringInvariant(this double value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Следующий месяц
        /// </summary>
        /// <param name="value">Дата</param>
        /// <returns>переданное значение + 1 месяц</returns>
        public static DateTime NextMonth(this DateTime value)
        {
            return value.AddMonths(1);
        }

        /// <summary>
        /// Предыдущий месяц
        /// </summary>
        /// <param name="value">Дата</param>
        /// <returns>Переданное значение -1 месяц</returns>
        public static DateTime PreviousMonth(this DateTime value)
        {
            return value.AddMonths(-1);
        }

        /// <summary>
        /// Удовлетворяет ли строка <paramref name="value"/> выражению <paramref name="pattern"/>.
        /// Используется для трансляции выражения в запрос NHibernate.
        /// </summary>
        /// <remarks>Выражение <paramref name="pattern"/> должно быть в формате SQL LIKE (с % и []_)</remarks>
        /// <param name="value">Строка</param>
        /// <param name="pattern">Выражение</param>
        /// <returns>true - удовлетворяет, false - иначе</returns>
        public static bool IsLike( this string value, string pattern )
        {
            pattern = Regex.Escape( pattern );
            pattern = pattern.Replace( "%", ".*?" ).Replace( "_", "." );
            pattern = pattern.Replace( @"\[", "[" ).Replace( @"\]", "]" ).Replace( @"\^", "^" );

            return Regex.IsMatch( value, pattern );
        }

        /// <summary>
        /// Реализация Safe Navigation Operator (?.)
        /// <para>Использовать этот вариант, если надо вернуть значение равное default( <typeparamref name="TResult"/> )</para>
        /// <para>Если надо вернуть значение отличное от default( <typeparamref name="TResult"/> ), использовать <see cref="IfNotNull{TIn,TResult}(TIn,System.Func{TIn,TResult},TResult)"/></para>
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="whenNotNull">Выражение, которое будет вызвано, если <paramref name="obj"/> != null</param>
        /// <typeparam name="TIn">Тип объекта</typeparam>
        /// <typeparam name="TResult">Тип возвращаемого значения из выражения <paramref name="whenNotNull"/></typeparam>
        /// <returns>default( TResult ), если <paramref name="obj"/> == null, иначе результат выражения <paramref name="whenNotNull"/></returns>
        public static TResult IfNotNull< TIn, TResult >( this TIn obj, Func< TIn, TResult > whenNotNull ) where TIn : class
        {
            return obj == null ? default( TResult ) : whenNotNull( obj );
        }

        /// <summary>
        /// Реализация Safe Navigation Operator (?.)
        /// <para>Использовать этот вариант, если надо вернуть значение отличное от default( <typeparamref name="TResult"/> )</para>
        /// <para>Если надо вернуть значение равное default( <typeparamref name="TResult"/> ), использовать <see cref="IfNotNull{TIn,TResult}(TIn,System.Func{TIn,TResult})"/></para>
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="whenNotNull">Выражение, которое будет вызвано, если <paramref name="obj"/> != null</param>
        /// <param name="whenNull">Значение, возвращаемое, если <paramref name="obj"/> == null</param>
        /// <typeparam name="TIn">Тип объекта</typeparam>
        /// <typeparam name="TResult">Тип возвращаемого значения из выражения <paramref name="whenNotNull"/></typeparam>
        /// <returns><paramref name="whenNull"/>, если <paramref name="obj"/> == null, иначе результат выражения <paramref name="whenNotNull"/></returns>
        public static TResult IfNotNull< TIn, TResult >( this TIn obj, Func< TIn, TResult > whenNotNull, TResult whenNull )
            where TIn : class 
            where TResult : struct
        {
            return obj == null ? whenNull : whenNotNull( obj );
        }

        /// <summary>
        /// Сокращение для <see cref="string.IsNullOrWhiteSpace"/>
        /// </summary>
        /// <param name="text">Проверяемая строка</param>
        /// <returns>Аналогично <see cref="string.IsNullOrWhiteSpace"/></returns>
        public static bool IsNullOrWhiteSpace( this string text )
        {
            return string.IsNullOrWhiteSpace( text );
        }

        /// <summary>
        /// Сокращение для <see cref="string.IsNullOrEmpty"/>
        /// </summary>
        /// <param name="text">Проверяемая строка</param>
        /// <returns>Аналогично <see cref="string.IsNullOrEmpty"/></returns>
        public static bool IsNullOrEmpty( this string text )
        {
            return string.IsNullOrEmpty( text );
        }

        /// <summary>
        /// Возвращает <paramref name="defaultValue"/>, если <paramref name="text"/> == null или <paramref name="text"/> состоит изнепечатаемых символов
        /// </summary>
        /// <param name="text">Строка</param>
        /// <param name="defaultValue">Что вернуть</param>
        /// <returns>Либо <paramref name="text"/>, либо <paramref name="defaultValue"/></returns>
        public static string IfNullOrEmpty( this string text, string defaultValue )
        {
            return string.IsNullOrWhiteSpace( text ) ? defaultValue : text;
        }

        /// <summary>
        /// Возвращает <paramref name="defaultValue"/>, если <paramref name="text"/> == null или <paramref name="text"/> пустая строка
        /// </summary>
        /// <param name="text">Строка</param>
        /// <param name="defaultValue">Что вернуть</param>
        /// <returns>Либо <paramref name="text"/>, либо <paramref name="defaultValue"/></returns>
        public static string IfNullOrWhiteSpace( this string text, string defaultValue )
        {
            return string.IsNullOrWhiteSpace( text ) ? defaultValue : text;
        }

        /// <summary>
        /// Проеверяет, является ли перечисление или коллекция пустой (или вообще null)
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="items">Перечисление элементов</param>
        /// <returns>true, если коллекция = null или в ней нет элементов </returns>
        public static bool IsNullOrEmpty<T>( this IEnumerable<T> items )
        {
            return items == null || !items.Any();
        }

        /// <summary>
        /// Форматрует строку с использованием инвариантной культуры (сокращение )
        /// </summary>
        /// <param name="format">Аналогично string.Format</param>
        /// <param name="args">Аналогично аргументам string.Format</param>
        /// <returns>см. string.Format, </returns>
        public static string FormatInvariant( this string format, params object[] args )
        {
            try
            {
                return format.IfNotNull( x => string.Format( CultureInfo.InvariantCulture, x, args ) ) ?? string.Empty;
            }
            catch( FormatException )
            {
                return format;
            }
        }
        
        /// <summary> Равны ли два вещественных числа (с учётом точности их хранения) </summary>
        /// <param name="value1">Значение 1</param>
        /// <param name="value2">Значение 2</param>
        /// <returns>Равны</returns>
        public static bool Same(this double value1, double value2)
        {
            //TODO: перед тем как использовать где-то еще - переписать, double.Epsilon не лучший вариант, см. https://msdn.microsoft.com/en-us/library/ya2zha7s(v=vs.110).aspx, "we recommend that you do not use Epsilon when comparing Double values for equality."
            return Math.Abs(value1 - value2) <= double.Epsilon;
        }


        /// <summary> Равны ли два показания (с учётом точности их хранения) </summary>
        /// <param name="value1">Значение 1</param>
        /// <param name="value2">Значение 2</param>
        /// <returns>Равны</returns>
        public static bool SameReadings(this double? value1, double? value2)
        {
            if( (value1 == null) || (value2==null))
            {
                return ( value1 ?? value2 ) == null;
            }
            return SameReadings( value1.Value, value2.Value );
        }

        /// <summary> Равны ли два показания (с учётом точности их хранения) </summary>
        /// <param name="value1">Значение 1</param>
        /// <param name="value2">Значение 2</param>
        /// <returns>Равны</returns>
        public static bool SameReadings(this double value1, double? value2)
        {
            if( value2 == null )
            {
                return false;
            }
            return SameReadings(value1, value2.Value);
        }

        /// <summary>
        /// Получить типизированный список атрибутов указанного типа
        /// </summary>
        /// <typeparam name="TAttribute">Тип атрибутов</typeparam>
        /// <param name="memberInfo">Тип, атрибуты которого нужно получить</param>
        /// <returns>Список атрибутов</returns>
        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>().ToArray();
        }

        /// <summary>
        /// Получить атрибут указанного типа, подразумевая, что такой атрибут может быть только один
        /// </summary>
        /// <typeparam name="TAttribute">Тип атрибутов</typeparam>
        /// <param name="memberInfo">Тип, атрибуты которого нужно получить</param>
        /// <returns>Экземпляр запрошенного атрибута или null</returns>
        public static TAttribute GetAttribute<TAttribute>(this MemberInfo memberInfo) where TAttribute : Attribute
        {
            return (TAttribute)memberInfo.GetCustomAttributes(typeof(TAttribute), true).SingleOrDefault();
        }

        /// <summary>
        /// добавить все элементы последовательности в set
        /// </summary>
        /// <typeparam name="T">тип элемента</typeparam>
        /// <param name="set">собственно set</param>
        /// <param name="additions">последовательность, все элeменты которой надо добавить в set</param>
        public static void AddAll< T >( this ISet< T > set, IEnumerable< T > additions )
        {
            additions.ForEach( _=>set.Add( _ ) );
        }

        /// <summary>
        /// преобразовать первый символ строки в upper case
        /// (тек. культура)
        /// </summary>
        /// <param name="input">входная строка</param>
        /// <returns>строку в которой первый символ переведен в заглавный; если input null или "", будет возврашен input же  </returns>
        public static string FirstLetterToUppercase( this string input )
        {
            if( string.IsNullOrEmpty( input ) )
            {
                return input;
            }
            if( input.Length > 1 )
            {
                return input[ 0 ].ToString().ToUpper() + input.Substring( 1 );
            }
            return input.ToUpper();
        }
        
        public static string FirstLetterToLowercase( this string input )
        {
            if( string.IsNullOrEmpty( input ) )
            {
                return input;
            }
            if( input.Length > 1 )
            {
                return input[ 0 ].ToString().ToLower() + input.Substring( 1 );
            }
            return input.ToUpper();
        }
    }
}
