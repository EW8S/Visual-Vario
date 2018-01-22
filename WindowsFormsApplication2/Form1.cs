using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.IO;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {

        static bool _continue;                              //Состояние порта
        static SerialPort _serialPort;                      //Эксземляр класса ком порт
        static byte[] uart_mass = new byte[0x1FFFFF];       //Применый массив
        static int uart_count;                              //Счетчик
        static string fileName;

        float varVario = 10.63f;
        float varProcess = 0.05f;
        float Pc = 0.0f;
        float G = 0.0f;
        float P = 1.0f;
        float Xp = 0.0f;
        float Zp = 0.0f;
        float Xe = 0.0f;

        static float ex_r, r;


        public Form1()
        {
            InitializeComponent();
        }
        //-----------------------------------------------------------  Выводим доступные COM port
        private void getPort()
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }
        }
        //----------------------------------------------------------  Обработчик события загрузки формы, потути наш main
        private void Form1_Load(object sender, EventArgs e)
        {
            getPort();      //Выводим в комбобокс доступные порты
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;     //Если они есть, выводим первый

            progressBar1.Minimum = 0;                                       //Настройки прогресс бара
            progressBar1.Maximum = 0x1FFFFF;
            progressBar1.Value = 0;

            // Chart
            //Задание маштаба по оси X
            //chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, 50);   //При старте от показаны будут точки только от 0 до 50
            //Включим возможность использования курсора по оси X
            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
            //Включим возможность мастабирования по выделенному интервалу
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            //Включим мастабирование по оси X
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            //Добавим полосу прокрутки
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].CursorX.Interval = 0.01;



            //Задание маштаба по оси Y
            chart1.ChartAreas[0].AxisY.ScaleView.Zoom(90000, 105000);   //При старте от показаны будут точки только от 0 до 50
            //Включим возможность использования курсора по оси Y
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
            //Включим возможность мастабирования по выделенному интервалу
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            //Включим мастабирование по оси Y
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            //Добавим полосу прокрутки
            chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;


            //chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            //chart2.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            
            }
        //---------------------------------------------------------  Обработчик нажатия кнопки
        private void button1_Click(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            Object selectedItem = comboBox1.SelectedItem;

            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort();

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = selectedItem.ToString();
            _serialPort.BaudRate = 115200;
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None", true);
            _serialPort.DataBits = 8;
            _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One", true);
            _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), "None", true);

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            _serialPort.Open();
            _continue = true;
            readThread.Start();
        }
        //-------------------------------------------------------------------------------------
        private void DisplayText(object sender, EventArgs e)
        {
            progressBar1.Value = uart_count;
        }
        //-------------------------------------------------------------------------------------
        private void saveData()
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                for (int i = 0; i < 0x1FFFFF; i++)
                {
                    writer.Write(uart_mass[i]);
                }
                writer.Write(true);
                writer.Close();
            }

            const string message = "Данные записаны и сохранены";
            const string caption = "Результат";
            MessageBox.Show(message, caption);
        }
        //-------------------------------------------------------------------------------------
        public void Read()    //Поток чтения из COM-port
        {
            while (_continue)
            {
                try
                {
                    int n_uart = _serialPort.BytesToRead;
                    if (n_uart > 0)
                    {
                        _serialPort.Read(uart_mass, uart_count, n_uart);
                        uart_count = uart_count + n_uart;
                        this.Invoke(new EventHandler(DisplayText));
                        if (uart_count == 0x1FFFFF) saveData();
                    }
                }
                catch (TimeoutException) { Application.Exit(); }
            }
        }
        //-----------------------------------------------------------  Кнопка считать данные
        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "dat files (*.dat)|*.dat|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileName = saveFileDialog1.FileName;
                for (int i = 0; i < 0x1FFFFF; i++) uart_mass[i] = 0xFF;
                uart_count = 0;
                if (_continue)
                {
                    _serialPort.Write("1\r");
                }
            }
            
            //-------------------------------------------------------------------------------------

        }
        //--------------Вывод данных их файла в массив ----
        private void button3_Click(object sender, EventArgs e)
        {
            //Из файла
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "dat files (*.dat)|*.dat|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog1.FileName;

                if (File.Exists(fileName))
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
                    {
                        uart_mass = reader.ReadBytes(0x1FFFFF);
                    }
                }
            }


        }
        //-------------------------------------------------------------  Филитр
        private long filter(float val)
        {  //функция фильтрации
            Pc = P + varProcess;
            G = Pc / (Pc + varVario);
            P = (1 - G) * Pc;
            Xp = Xe;
            Zp = Xp;
            Xe = G * (val - Zp) + Xp; // "фильтрованное" значение
            return ((long)Xe);
        }
        //-----------------------------------------------------------  Вывод давления
        private void button4_Click(object sender, EventArgs e)
        {
            int findPoint;

            // Поиск завершения реальных данных
            for (findPoint=0; findPoint<0x1FFFF0; findPoint = findPoint + 4)
            {
                if ((uart_mass[findPoint] == 0xFF) && (uart_mass[findPoint + 1] == 0xFF))
                    break;
            }

            //Выисление длиныы массива для сырых данных от датчика
            int len16bit = (findPoint)/4;

            chart1.Series[0].Points.Clear();

            
            for (long w = 0; w < len16bit; w++)
            {
                float timSec = 0.105f * w;
                uint daw = uart_mass[(w * 4)];
                daw = daw << 8;
                daw = daw | uart_mass[(w * 4) + 1];
                daw = daw << 8;
                daw = daw | uart_mass[(w * 4) + 2];
                daw = daw << 8;
                daw = daw | uart_mass[(w * 4) + 3]; 
                float filtered = filter(daw);
                chart1.Series[0].Points.AddXY(timSec, daw);
                chart1.Series[1].Points.AddXY(timSec, filtered);
                if (w == 0)
                {
                    ex_r = filtered;
                    chart2.Series[0].Points.AddXY(timSec, 0);
                }
                else
                {
                    float del = filtered - ex_r;
                    ex_r = filtered;
                    chart2.Series[0].Points.AddXY(timSec, del);
                }
                
                
            }

        }
        //-----------------------------------------------------------
        private void button5_Click(object sender, EventArgs e)
        {
            float P1 = float.Parse(textBox1.Text);
            float P2 = float.Parse(textBox2.Text);
            double H = 18400 * (1 + 0.003665f) * Math.Log10(P1 / P2);
            textBox3.Text = string.Format("{0:0.##}", H) + " м";
            float T1 = float.Parse(textBox4.Text);
            float T2 = float.Parse(textBox5.Text);
            float T3 = T2 - T1;
            float S = (float)H / T3;
            textBox6.Text = string.Format("{0:0.##}", S)+" м/с";
            textBox7.Text = string.Format("{0:0.##}", T3) + " с";
        }

        private void chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            //
            double minX = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
            double maxX = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
            //chart2.ChartAreas[0].AxisX.ScaleView.Zoom(minX, maxX);

            double minY = chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum;
            double maxY = chart1.ChartAreas[0].AxisY.ScaleView.ViewMaximum;
            //chart2.ChartAreas[0].AxisY.ScaleView.Zoom(minY, maxY);

            //chart1.ChartAreas[0].AxisX.
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F1)
            {
                textBox1.Text = chart1.ChartAreas[0].CursorY.Position.ToString();
                textBox4.Text = chart1.ChartAreas[0].CursorX.Position.ToString();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox2.Text = chart1.ChartAreas[0].CursorY.Position.ToString();
                textBox5.Text = chart1.ChartAreas[0].CursorX.Position.ToString();

                float P1 = float.Parse(textBox1.Text);
                float P2 = float.Parse(textBox2.Text);
                double H = 18400 * (1 + 0.003665f) * Math.Log10(P1 / P2);
                textBox3.Text = string.Format("{0:0.##}", H) + " м";
                float T1 = float.Parse(textBox4.Text);
                float T2 = float.Parse(textBox5.Text);
                float T3 = T2 - T1;
                float S = (float)H / T3;
                textBox6.Text = string.Format("{0:0.##}", S) + " м/с";
                textBox7.Text = string.Format("{0:0.##}", T3) + " с";
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox1.Text = chart1.ChartAreas[0].CursorY.Position.ToString();
                textBox4.Text = chart1.ChartAreas[0].CursorX.Position.ToString();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox2.Text = chart1.ChartAreas[0].CursorY.Position.ToString();
                textBox5.Text = chart1.ChartAreas[0].CursorX.Position.ToString();

                float P1 = float.Parse(textBox1.Text);
                float P2 = float.Parse(textBox2.Text);
                double H = 18400 * (1 + 0.003665f) * Math.Log10(P1 / P2);
                textBox3.Text = string.Format("{0:0.##}", H) + " м";
                float T1 = float.Parse(textBox4.Text);
                float T2 = float.Parse(textBox5.Text);
                float T3 = T2 - T1;
                float S = (float)H / T3;
                textBox6.Text = string.Format("{0:0.##}", S) + " м/с";
                textBox7.Text = string.Format("{0:0.##}", T3) + " с";
            }
        }
    }
}
