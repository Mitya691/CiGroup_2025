using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sharp7;

namespace InImArchiverService
{
    /// <summary>
    /// Функции для работы с адресами Step 7.
    /// Редакция от 05 июня 2020 г.
    /// Алексей Оводков
    /// </summary>
    public static class S7Utils
    {

        /// <summary>
        /// Проверка адреса S7.
        /// </summary>
        /// <param name="address">Адрес переменной в нотации Step 7, который нужно проверить.</param>
        internal static bool S7AddressCheck(string address)
        {
            MatchCollection test;
            string a = address.ToLower();
            switch (a[0])
            {
                //блок данных, число DBXXXX.DBYZZZZZ - \bdb(\d{1,5}).db[bwd](\d{1,5})\b
                //блок данных, бит DBXXXX.DBYZZZZZ.T - \bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b
                case 'd':
                    if (a[1] != 'b') return false;
                    //блок данных, число DBXXXX.DBYZZZZZ - \bdb(\d{1,5}).db[bwd](\d{1,5})\b
                    test = Regex.Matches(a, @"\bdb(\d{1,5}).db[bwd](\d{1,5})\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 2)
                        {
                            foreach (Match match in test)
                                if (Convert.ToInt32(match.Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    //блок данных, бит DBXXXX.DBYZZZZZ.T - \bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b
                    test = Regex.Matches(a, @"\bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 3)
                        {
                            for (int i = 0; i < 2; i++)
                                if (Convert.ToInt32(test[i].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    return false;
                //периферийный вход-выход, число PXYZZZZZ - \bp[iq][bwd](\d{1,5})\b
                //периферийный вход-выход, бит PXYZZZZZ.T - \bp[iq](\d{1,5}).[01234567]\b
                case 'p':
                    //периферийный вход-выход, число PXYZZZZZ - \bp[iq][bwd](\d{1,5})\b
                    test = Regex.Matches(a, @"\bp[iq][bwd](\d{1,5})\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 1)
                        {
                            if (Convert.ToInt32(test[0].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    //периферийный вход-выход, бит PXYZZZZZ.T - \bp[iq](\d{1,5}).[01234567]\b
                    test = Regex.Matches(a, @"\bp[iq](\d{1,5}).[01234567]\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 2)
                        {
                            if (Convert.ToInt32(test[0].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    return false;
                //меркер и вход-выход, число XYZZZZZ - \b[iqm][bwd](\d{1,5})\b
                //меркер и вход-выход, бит XYZZZZZ.T - \b[iqm](\d{1,5}).[01234567]\b
                case 'i':
                case 'q':
                case 'm':
                    //меркер и вход-выход, число XYZZZZZ - \b[iqm][bwd](\d{1,5})\b
                    test = Regex.Matches(a, @"\b[iqm][bwd](\d{1,5})\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 1)
                        {
                            if (Convert.ToInt32(test[0].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    //меркер и вход-выход, бит XYZZZZZ.T - \b[iqm](\d{1,5}).[01234567]\b
                    test = Regex.Matches(a, @"\b[iqm](\d{1,5}).[01234567]\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 2)
                        {
                            if (Convert.ToInt32(test[0].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    return false;
                //таймер или счётчик XYYYYY - \b[tc](\d{1,5})\b
                case 't':
                case 'c':
                    //таймер или счётчик XYYYYY - \b[tc](\d{1,5})\b
                    test = Regex.Matches(a, @"\b[tc](\d{1,5})\b");
                    if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    {
                        //проверить числа, входящие в адрес
                        test = Regex.Matches(a, @"\d{1,5}");
                        if (test.Count == 1)
                        {
                            if (Convert.ToInt32(test[0].Value) > ushort.MaxValue) return false;
                            return true;
                        }
                        return false;
                    }
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Проверка адреса S7 на соотвтствие типу дотнет.
        /// </summary>
        /// <param name="address">Валидный адрес переменной в нотации Step 7, который нужно проверить.</param>
        /// <param name="type">Тип, на соответствие которому проверяется адрес.</param>
        internal static bool S7TypeCheck(string address, Type type)
        {
            MatchCollection test;
            string a = address.ToLower();

            //бит
            if (type == typeof(bool))
            {
                //блок данных, бит DBXXXX.DBYZZZZZ.T - \bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //периферийный вход-выход, бит PXYZZZZZ.T - \bp[iq](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bp[iq](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //меркер и вход-выход, бит XYZZZZZ.T - \b[iqm](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\b[iqm](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                return false;
            }

            //байт
            if (type == typeof(byte) || type == typeof(sbyte))
            {
                //блок данных, число DBXXXX.DBBZZZZZ - \bdb(\d{1,5}).dbb(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbb(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //периферийный вход-выход, число PXBZZZZZ - \bp[iq]b(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //меркер и вход-выход, число XBZZZZZ - \b[iqm]b(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                return false;
            }

            //слово
            if (type == typeof(ushort) || type == typeof(short))
            {
                //таймер или счётчик XYYYYY - \b[tc](\d{1,5})\b (таймер и счётчик преобразовываются только в беззнаковое слово)
                test = Regex.Matches(a, @"\b[tc](\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                    return type == typeof(ushort);
                //блок данных, число DBXXXX.DBWZZZZZ - \bdb(\d{1,5}).dbw(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbw(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //периферийный вход-выход, число PXWZZZZZ - \bp[iq]w(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //меркер и вход-выход, число XWZZZZZ - \b[iqm]w(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                return false;
            }

            //двойное слово или переменная с плавающей точкой одинарной точности
            if (type == typeof(uint) || type == typeof(int) || type == typeof(float))
            {
                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture)) return true;
                return false;
            }

            //выбранный тип не поддерживается
            return false;
        }

        /// <summary>
        /// Преобразование адреса S7 в адрес OPC-UA без префикса.
        /// </summary>
        /// <param name="address">Валидный адрес переменной в нотации Step 7, который нужно преобразовать.</param>
        /// <param name="type">Соответствующий разрядности адреса тип.</param>
        internal static string S7ToUaAddress(string address, Type type)
        {
            MatchCollection test;
            string a = address.ToLower();

            //бит
            if (type == typeof(bool))
            {
                //блок данных, бит DBXXXX.DBYZZZZZ.T - \bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBX0.1 -> DB515.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",x" + test[2].Value;
                }
                //периферийный вход-выход, бит PXYZZZZZ.T - \bp[iq](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bp[iq](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PI0.1 -> PI.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",x" + test[1].Value;
                }
                //меркер и вход-выход, бит XYZZZZZ.T - \b[iqm](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\b[iqm](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: I0.1 -> I.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",x" + test[1].Value;
                }
                return null;
            }

            //байт, целое беззнаковое
            if (type == typeof(byte))
            {
                //блок данных, число DBXXXX.DBBZZZZZ - \bdb(\d{1,5}).dbb(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbb(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBB0 -> DB515.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",b";
                }
                //периферийный вход-выход, число PXBZZZZZ - \bp[iq]b(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIB0 -> PI.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",b";
                }
                //меркер и вход-выход, число XBZZZZZ - \b[iqm]b(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IB0 -> I.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",b";
                }
                return null;
            }

            //байт, целое знаковое
            if (type == typeof(sbyte))
            {
                //блок данных, число DBXXXX.DBBZZZZZ - \bdb(\d{1,5}).dbb(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbb(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBB0 -> DB515.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",c";
                }
                //периферийный вход-выход, число PXBZZZZZ - \bp[iq]b(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIB0 -> PI.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",c";
                }
                //меркер и вход-выход, число XBZZZZZ - \b[iqm]b(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IB0 -> I.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",c";
                }
                return null;
            }

            //слово, целое беззнаковое
            if (type == typeof(ushort))
            {
                //блок данных, число DBXXXX.DBWZZZZZ - \bdb(\d{1,5}).dbw(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbw(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBW0 -> DB515.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",w";
                }
                //периферийный вход-выход, число PXWZZZZZ - \bp[iq]w(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIW0 -> PI.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",w";
                }
                //меркер и вход-выход, число XWZZZZZ - \b[iqm]w(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IW0 -> I.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",w";
                }
                //таймер XYYYYY - \bt(\d{1,5})\b
                test = Regex.Matches(a, @"\bt(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: T10 -> t.10,tbcd
                    return "t." + a.Substring(1) + ",tbcd";
                }
                //счётчик XYYYYY - \bc(\d{1,5})\b
                test = Regex.Matches(a, @"\bc(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: C10 -> c.10,c
                    return "c." + a.Substring(1) + ",c";
                }

                return null;
            }

            //слово, целое знаковое
            if (type == typeof(short))
            {
                //блок данных, число DBXXXX.DBWZZZZZ - \bdb(\d{1,5}).dbw(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbw(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBW0 -> DB515.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",i";
                }
                //периферийный вход-выход, число PXWZZZZZ - \bp[iq]w(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIW0 -> PI.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",i";
                }
                //меркер и вход-выход, число XWZZZZZ - \b[iqm]w(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IW0 -> I.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",i";
                }
                return null;
            }

            //двойное слово, целое беззнаковое
            if (type == typeof(uint))
            {
                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",dw";
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",dw";
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",dw";
                }
                return null;
            }

            //двойное слово, целое знаковое
            if (type == typeof(int))
            {
                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",di";
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",di";
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",di";
                }
                return null;
            }

            //переменная с плавающей точкой одинарной точности
            if (type == typeof(float))
            {
                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    return "DB" + test[0].Value + "." + test[1].Value + ",r";
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 2).ToUpper() + "." + test[0].Value + ",r";
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    return a.Substring(0, 1).ToUpper() + "." + test[0].Value + ",r";
                }
                return null;
            }

            //выбранный тип не поддерживается
            return null;
        }


        /// <summary>
        /// Преобразование адреса S7 в набор данных для создания тега библиотеки Sharp7. При ошибке возвращает null.
        /// Состав возвращаемого массива данных:
        /// result[0] - Area;
        /// result[1] - WordLen;
        /// result[2] - DbNumber;
        /// result[3] - Start;
        /// </summary>
        /// <param name="address">Валидный адрес переменной в нотации Step 7, который нужно преобразовать.</param>
        /// <param name="type">Соответствующий разрядности адреса тип.</param>
        /// <returns>
        /// Состав возвращаемого массива данных:
        /// result[0] - Area;
        /// result[1] - WordLen;
        /// result[2] - DbNumber;
        /// result[3] - Start;
        /// </returns>
        internal static int[] S7ToSharp7Address(string address, Type type)
        {
            MatchCollection test;
            string a = address.ToLower();
            int[] result = new int[] { 0, 0, 0, 0 };

            //бит
            if (type == typeof(bool))
            {
                //длна области данных - бит
                result[1] = S7Consts.S7WlBit;

                //блок данных, бит DBXXXX.DBYZZZZZ.T - \bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbx(\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBX0.1 -> DB515.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value) * 8 + Convert.ToInt32(test[2].Value);
                    return result;
                }
                //периферийный вход-выход, бит PXYZZZZZ.T - \bp[iq](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\bp[iq](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PI0.1 -> PI.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value) * 8 + Convert.ToInt32(test[1].Value);
                    return result;
                }
                //меркер и вход-выход, бит XYZZZZZ.T - \b[iqm](\d{1,5}).[01234567]\b
                test = Regex.Matches(a, @"\b[iqm](\d{1,5}).[01234567]\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: I0.1 -> I.0,x1
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value) * 8 + Convert.ToInt32(test[1].Value);
                    return result;
                }
                return null;
            }

            //байт, целое беззнаковое
            if (type == typeof(byte))
            {
                //длна области данных - байт
                result[1] = S7Consts.S7WlByte;

                //блок данных, число DBXXXX.DBBZZZZZ - \bdb(\d{1,5}).dbb(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbb(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBB0 -> DB515.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXBZZZZZ - \bp[iq]b(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIB0 -> PI.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XBZZZZZ - \b[iqm]b(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IB0 -> I.0,b
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //байт, целое знаковое
            if (type == typeof(sbyte))
            {
                //длна области данных - байт
                result[1] = S7Consts.S7WlChar;

                //блок данных, число DBXXXX.DBBZZZZZ - \bdb(\d{1,5}).dbb(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbb(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBB0 -> DB515.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXBZZZZZ - \bp[iq]b(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIB0 -> PI.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XBZZZZZ - \b[iqm]b(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]b(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IB0 -> I.0,c
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //слово, целое беззнаковое
            if (type == typeof(ushort))
            {
                //длна области данных - слово беззнаковое
                result[1] = S7Consts.S7WlWord;

                //блок данных, число DBXXXX.DBWZZZZZ - \bdb(\d{1,5}).dbw(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbw(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBW0 -> DB515.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXWZZZZZ - \bp[iq]w(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIW0 -> PI.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XWZZZZZ - \b[iqm]w(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IW0 -> I.0,w
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //таймер XYYYYY - \bt(\d{1,5})\b
                test = Regex.Matches(a, @"\bt(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: T10 -> t.10,tbcd
                    result[0] = S7Consts.S7AreaT;
                    result[3] = Convert.ToInt32(a.Substring(1));
                    return result;
                }
                //счётчик XYYYYY - \bc(\d{1,5})\b
                test = Regex.Matches(a, @"\bc(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: C10 -> c.10,c
                    result[0] = S7Consts.S7AreaC;
                    result[3] = Convert.ToInt32(a.Substring(1));
                    return result;
                }

                return null;
            }

            //слово, целое знаковое
            if (type == typeof(short))
            {
                //длна области данных - слово знаковое
                result[1] = S7Consts.S7WlInt;

                //блок данных, число DBXXXX.DBWZZZZZ - \bdb(\d{1,5}).dbw(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbw(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBW0 -> DB515.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXWZZZZZ - \bp[iq]w(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PIW0 -> PI.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XWZZZZZ - \b[iqm]w(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]w(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: IW0 -> I.0,i
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //двойное слово, целое беззнаковое
            if (type == typeof(uint))
            {
                //длна области данных - двойное слово беззнаковое
                result[1] = S7Consts.S7WldWord;

                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,dw
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //двойное слово, целое знаковое
            if (type == typeof(int))
            {
                //длна области данных - двойное слово знаковое
                result[1] = S7Consts.S7WldInt;

                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,di
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //переменная с плавающей точкой одинарной точности
            if (type == typeof(float))
            {
                //длна области данных - с плавающей точкой одинарной точности
                result[1] = S7Consts.S7WlReal;

                //блок данных, число DBXXXX.DBDZZZZZ - \bdb(\d{1,5}).dbd(\d{1,5})\b
                test = Regex.Matches(a, @"\bdb(\d{1,5}).dbd(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: DB515.DBDW0 -> DB515.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaDb;
                    result[2] = Convert.ToInt32(test[0].Value);
                    result[3] = Convert.ToInt32(test[1].Value);
                    return result;
                }
                //периферийный вход-выход, число PXDZZZZZ - \bp[iq]d(\d{1,5})\b
                test = Regex.Matches(a, @"\bp[iq]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: PID0 -> PI.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    result[0] = S7Consts.S7AreaP;
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                //меркер и вход-выход, число XDZZZZZ - \b[iqm]d(\d{1,5})\b
                test = Regex.Matches(a, @"\b[iqm]d(\d{1,5})\b");
                if (test.Count == 1 && string.Equals(test[0].Value, a, StringComparison.InvariantCulture))
                {
                    //выделить все цифры и вернуть UA-адрес
                    //ПРИМЕР: ID0 -> I.0,r
                    test = Regex.Matches(a, @"\d{1,5}");
                    switch (a.Substring(0, 1).ToUpper())
                    {
                        case "I":
                            result[0] = S7Consts.S7AreaI;
                            break;
                        case "Q":
                            result[0] = S7Consts.S7AreaQ;
                            break;
                        case "M":
                            result[0] = S7Consts.S7AreaM;
                            break;
                    }
                    result[3] = Convert.ToInt32(test[0].Value);
                    return result;
                }
                return null;
            }

            //выбранный тип не поддерживается
            return null;
        }

        /// <summary>
        /// Преобразовать переменную в байты для записи в контроллер S7.
        /// </summary>
        /// <param name="value">Значение, байты которого нужно получить.</param>
        public static byte[] ValueToS7Bytes(object value)
        {
            byte[] bytes = null;

            if (value is bool)
                return BitConverter.GetBytes((bool)value);

            if (value is byte)
                return BitConverter.GetBytes((byte)value);

            //знаковый байт битконвертер конвертирует в массив из двух байт
            if (value is sbyte)
                return new[] { BitConverter.GetBytes((sbyte)value)[0] };

            if (value is short)
                bytes = BitConverter.GetBytes((short)value);

            if (value is ushort)
                bytes = BitConverter.GetBytes((ushort)value);

            if (value is int)
                bytes = BitConverter.GetBytes((int)value);

            if (value is uint)
                bytes = BitConverter.GetBytes((uint)value);

            if (value is float)
                bytes = BitConverter.GetBytes((float)value);

            if (bytes == null) return null;

            return bytes.Length == 2 ? new[] { bytes[1], bytes[0] } : new[] { bytes[3], bytes[2], bytes[1], bytes[0] };
        }

        /// <summary>
        /// Преобразовать переменную в байты для записи в контроллер S7.
        /// </summary>
        /// <param name="bytes">Байтовый массив со значениями в формате S7.</param>
        /// <param name="type">Требуемый тип, к которому нужно выполнить преобразование.</param>
        /// <param name="offset">Смещение от начала массива.</param>
        public static object S7BytesToValue(byte[] bytes, Type type, int offset = 0)
        {

            if (type == typeof(bool))
                return bytes[offset] == 1;

            if (type == typeof(byte))
                return bytes[offset];

            if (type == typeof(sbyte))
            {
                //прямой конвертации беззнакового байта в знаковый не существует
                return bytes[offset] > 127
                    ? (sbyte)BitConverter.ToInt16(new[] { bytes[offset], (byte)255 }, 0)
                    : (sbyte)bytes[offset];
            }

            if (type == typeof(short))
                return BitConverter.ToInt16(new[] { bytes[offset + 1], bytes[offset] }, 0);

            if (type == typeof(ushort))
                return BitConverter.ToUInt16(new[] { bytes[offset + 1], bytes[offset] }, 0);

            if (type == typeof(int))
                return BitConverter.ToInt32(new[] { bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset] }, 0);

            if (type == typeof(uint))
                return BitConverter.ToUInt32(new[] { bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset] }, 0);

            if (type == typeof(float))
                return BitConverter.ToSingle(new[] { bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset] }, 0);

            return "Read Error";
        }
    }
}
