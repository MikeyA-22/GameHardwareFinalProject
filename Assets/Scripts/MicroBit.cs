using System;
using System.IO.Ports;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroBit : MonoBehaviour
{
    /* #region Static enums */
    public enum CommandType {SETUSE, SETPIXEL, WRITEANALOG, WRITEDIGITAL};
    public enum COMPort {COM1, COM2, COM3, COM4, COM5, COM6, COM7, COM8, COM9};
    /* #endRegion */
    /* #region Connection Settings */
    private volatile bool serialLock;
    private SerialPort serial;
    private Thread handleThread;
    public COMPort COM = COMPort.COM3;
    public int BAUD = 115200;
    private bool connected;
    public bool Connected
    {
        get{ return this.connected; }
    }
    /* #endRegion */
    /* #region Thread Managment fields */
    private volatile bool needUpdate;
    private volatile bool end;
    /* #endRegion */
    /* #region Time */
    private bool useTime;
    public bool UseTime
    {
        get{ return this.useTime; }
        set
        {
            if(value != this.useTime)
            {
                this.needUpdate = true;
                this.useTime = value;
            }
        }
    }
    private volatile int time;
    public int Time
    {
        get
        {
            if(!this.useTime)throw new System.ArgumentException("Port is not configured to use Time", "original");
            return this.time;
        }
    }
    /* #endRegion */
    /* #region Buttons */
    private bool useButtons;
    public bool UseButtons
    {
        get{ return this.useButtons; }
        set
        {
            if(value != this.useButtons)
            {
                this.needUpdate = true;
                this.useButtons = value;
            }
        }
    }
    private volatile bool a;
    public bool A
    {
        get
        {
            if(!this.useButtons)throw new System.ArgumentException("Port is not configured to use buttons", "original");
            return this.a;
        }
    }
    private volatile bool b;
    public bool B
    {
        get
        {
            if(!this.useButtons)throw new System.ArgumentException("Port is not configured to use Buttons", "original");
            return this.b;
        }
    }
    /* #endRegion */
    /* #region Compass */
    private bool useCompass;
    public bool UseCompass
    {
        get{ return this.useCompass; }
        set
        {
            if(value != this.useCompass)
            {
                this.needUpdate = true;
                this.useCompass = value;
            }
        }
    }
    private volatile int compass;
    public int Compass
    {
        get
        {
            if(!this.useCompass)throw new System.ArgumentException("Port is not configured to use Compass", "original");
            return this.compass;
        }
    }
    /* #endRegion */
    /* #region Accelerometer */
    private bool useAccelerometer;
    public bool UseAccelerometer
    {
        get{ return this.useAccelerometer; }
        set
        {
            if(value != this.useAccelerometer)
            {
                this.needUpdate = true;
                this.useAccelerometer = value;
            }
        }
    }
    private volatile int x;
    public int X
    {
        get
        {
            if(!this.useAccelerometer)throw new System.ArgumentException("Port is not configured to use Accelerometer", "original");
            return this.x;
        }
    }
    private volatile int y;
    public int Y
    {
        get
        {
            if(!this.useAccelerometer)throw new System.ArgumentException("Port is not configured to use Accelerometer", "original");
            return this.y;
        }
    }
    private volatile int z;
    public int Z
    {
        get
        {
            if(!this.useAccelerometer)throw new System.ArgumentException("Port is not configured to use Accelerometer", "original");
            return this.z;
        }
    }
    /* #endRegion */

    void Start()
    {
        this.useTime = false;
        this.useButtons = true;
        this.useCompass = false;
        this.useAccelerometer = false;
        this.serialLock = true;
        this.connected = false;
        this.init();
    }
    void OnApplicationQuit()
    {
        this.disconnect();
    }
    void init()
    {
        this.needUpdate = true;
        this.connected = true;
        this.end = false;
        this.serial = new SerialPort(this.COM.ToString(), this.BAUD);
        this.serial.Open();
        this.handleThread = new Thread(this.threadLoop);
        this.handleThread.Start();
    }
    private void close()
    {
        this.serial.Close();
        this.connected = false;
    }
    private void threadLoop()
    {
        while(!this.end)
        {
            if(this.needUpdate)
            {
                this.setUse();
                this.needUpdate = false;
            }
            string[] line = this.serial.ReadLine().Split(',');
            int currentIndex = 0;
            try
            {
                if(this.UseTime)
                {
                    this.time = Convert.ToInt32(line[currentIndex]);
                    currentIndex ++;
                }
                if(this.UseButtons)
                {
                    this.a = line[currentIndex] == "1";
                    currentIndex ++;
                    this.b = line[currentIndex] == "1";
                    currentIndex ++;
                }
                if(this.UseCompass)
                {
                    this.compass = Convert.ToInt32(line[currentIndex]);
                    currentIndex ++;
                }
                if(this.UseAccelerometer)
                {
                    this.x = Convert.ToInt32(line[currentIndex]);
                    currentIndex ++;
                    this.y = Convert.ToInt32(line[currentIndex]);
                    currentIndex ++;
                    this.z = Convert.ToInt32(line[currentIndex]);
                    currentIndex ++;
                }
            }
            catch (Exception) 
            {
                this.needUpdate = true;
            }
        }
        this.close();
    }
    private void send(CommandType command, string[] args)
    {
        while(!this.serialLock);
        this.serialLock = false;
        StringBuilder toSend = new StringBuilder((int)command + ",");
        foreach(string token in args)
        {
            toSend.Append(token);
            toSend.Append(",");
        }
        Debug.Log(toSend.ToString());
        this.serial.WriteLine(toSend.ToString());
        this.serialLock = true;
    }
    /* #region Public utilities */
    public void setUse()
    {
        setUse(this.UseTime, this.UseButtons, this.UseCompass, this.UseAccelerometer);
    }
    public void setUse(bool UseTime, bool UseButtons, bool UseCompass, bool UseAccelerometer)
    {
        this.UseTime = UseTime;
        this.UseButtons = UseButtons;
        this.UseCompass = UseCompass;
        this.UseAccelerometer = UseAccelerometer;
        string[] args = {UseTime ? "1":"0", UseButtons ? "1":"0", UseCompass ? "1":"0", UseAccelerometer ? "1":"0", "0", "0"};
        this.send(CommandType.SETUSE, args);
    }
    public void writeAnalogPin(int id, int val)
    {
        string[] args = {id.ToString(), val.ToString()};
        this.send(CommandType.WRITEANALOG, args);
    }
    public void writeDigitalPin(int id, bool val)
    {
        string[] args = {id.ToString(), val.ToString()};
        this.send(CommandType.WRITEDIGITAL, args);
    }
    public void setPixel(int X, int Y, int brightness)
    {
        string[] args = {X.ToString(), Y.ToString(), brightness.ToString()};
        this.send(CommandType.SETPIXEL, args);
    }
    public void disconnect()
    {
        this.end = true;
    }
    /* #endRegion */
}