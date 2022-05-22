using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Sent from server 
public enum ServerPackets
{
    accept,
    refuse,
    joinedRoom,
    disconnected,
    playerJoined,
    playerLeft,
    playerRespawn,
    playerFinish,
    playerCorrection,
    playerGrab,
    playerLetGo,
    playerPush,
    map,
    startGame,
    endGame,
    serverTick,
    serverClock,
    pong
}

// Sent from client
public enum ClientPackets
{
    introduction = 18,
    playerMovement,
    playerRespawn,
    playerReady,
    playerGrab,
    playerLetGo,
    playerPush,
    ping
}


/// <summary> Contains the packet methods used for high-level and low-level convertions.</summary>
public class Packet : IDisposable
{
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPosition;

    #region Constructors
    public Packet() // Not used in the system
    {
        buffer = new List<byte>(); // Intitialize buffer
        readPosition = 0; // Set readPos to 0
    }

    public Packet(int _id)
    {
        buffer = new List<byte>();
        readPosition = 0;

        Add(_id); // Write packet id to the buffer
    }

    public Packet(byte[] _data)
    {
        buffer = new List<byte>();
        readPosition = 0;

        SetBytes(_data);
    }

    public Packet(byte[] _data, int _readPosition = 0)
    {
        buffer = new List<byte>();
        readPosition = _readPosition;

        SetBytes(_data);
    }
    #endregion

    #region Write Data Into Packet
    public void Add(byte[] _value)
    {
        buffer.AddRange(_value);
    }

    public void Add(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void Add(int _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void Add(long _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void Add(Guid _value)
    {
        Add(_value.ToString().Length); // Add length of guid string
        buffer.AddRange(Encoding.ASCII.GetBytes(_value.ToString())); // Add guid string
    }

    public void Add(float _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void Add(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void Add(string _value)
    {
        Add(_value.Length); // Add length of string
        buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add string
    }

    public void Add(Vector3 _value)
    {
        Add(_value.x);
        Add(_value.y);
        Add(_value.z);
    }

    public void Add(Quaternion _value)
    {
        Add(_value.x);
        Add(_value.y);
        Add(_value.z);
        Add(_value.w);
    }
    #endregion

    #region Read Data From Packet
    public byte[] GetBytes(int _length, bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            byte[] _value = buffer.GetRange(readPosition, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPosition += _length; // Increase readPos by _length
            }
            return _value; // Return the bytes
        }
        else
        {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    public short GetShort(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            short _value = BitConverter.ToInt16(readableBuffer, readPosition); // Convert the bytes to a short
            if (_moveReadPos)
            {
                // If _moveReadPos is true and there are unread bytes
                readPosition += 2; // Increase readPos by 2
            }
            return _value; // Return the short
        }
        else
        {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    public int GetInt(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            int _value = BitConverter.ToInt32(readableBuffer, readPosition); // Convert the bytes to an int
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPosition += 4; // Increase readPos by 4
            }
            return _value; // Return the int
        }
        else
        {
            throw new Exception("Could not read value of type 'int'!");
        }
    }

    public long GetLong(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            long _value = BitConverter.ToInt64(readableBuffer, readPosition); // Convert the bytes to a long
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPosition += 8; // Increase readPos by 8
            }
            return _value; // Return the long
        }
        else
        {
            throw new Exception("Could not read value of type 'long'!");
        }
    }

    public float GetFloat(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            float _value = BitConverter.ToSingle(readableBuffer, readPosition); // Convert the bytes to a float
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPosition += 4; // Increase readPos by 4
            }
            return _value; // Return the float
        }
        else
        {
            throw new Exception("Could not read value of type 'float'!");
        }
    }

    public Guid GetGuid(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            string strHex = GetString(_moveReadPos); // Convert the bytes to a string
            Guid _value = new Guid(strHex); // Convert the string to a guid
            return _value; // Return the long
        }
        else
        {
            throw new Exception("Could not read value of type 'Guid'!");
        }
    }

    public bool GetBool(bool _moveReadPos = true)
    {
        if (buffer.Count > readPosition)
        {
            // If there are unread bytes
            bool _value = BitConverter.ToBoolean(readableBuffer, readPosition); // Convert the bytes to a bool
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPosition += 1; // Increase readPos by 1
            }
            return _value; // Return the bool
        }
        else
        {
            throw new Exception("Could not read value of type 'bool'!");
        }
    }

    public string GetString(bool _moveReadPos = true)
    {
        try
        {
            int _length = GetInt(); // Get the length of the string
            string _value = Encoding.ASCII.GetString(readableBuffer, readPosition, _length); // Convert the bytes to a string
            if (_moveReadPos && _value.Length > 0)
            {
                // If _moveReadPos is true string is not empty
                readPosition += _length; // Increase readPos by the length of the string
            }
            return _value; // Return the string
        }
        catch
        {
            throw new Exception("Could not read value of type 'string'!");
        }
    }

    public Vector3 GetVector3(bool _movedReadPos = true)
    {
        return new Vector3(GetFloat(_movedReadPos), GetFloat(_movedReadPos), GetFloat(_movedReadPos));
    }

    public Quaternion GetQuaternion(bool _movedReadPos = true)
    {
        return new Quaternion(GetFloat(_movedReadPos), GetFloat(_movedReadPos), GetFloat(_movedReadPos), GetFloat(_movedReadPos));
    }
    #endregion

    #region Functions
    /// <summary>Sets the packet's content and prepares it to be read.</summary>
    public void SetBytes(byte[] _data)
    {
        Add(_data);
        readableBuffer = buffer.ToArray();
    }

    /// <summary>Inserts the length of the packet's data at the start of the buffer.</summary>
    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
    }

    /// <summary>Inserts the given int at the start of the buffer.</summary>
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value));
    }

    /// <summary>Inserts the given Guid at the start of the buffer.</summary>
    public void InsertGuid(Guid _value)
    {
        buffer.InsertRange(0, _value.ToByteArray());
    }

    /// <summary>Gets the packet's fata in array form.</summary>
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }

    /// <summary>Gets the length of the packet's data.</summary>
    public int Length()
    {
        return buffer.Count;
    }

    /// <summary>Gets the length of the unread data contained in the packet.</summary>
    public int UnreadLength()
    {
        return Length() - readPosition;
    }

    /// <summary>Resets the packet instance to allow it to be reused.</summary>
    public void Reset(bool _shouldReset = true)
    {
        if (_shouldReset)
        {
            buffer.Clear(); // Clear buffer
            readableBuffer = null;
            readPosition = 0; // Reset readPos
        }
        else
        {
            readPosition -= 4; // "Unread" the last read int
        }
    }
    #endregion

    #region IDisposable interface
    private bool disposed = false;

    protected virtual void Dispose(bool _disposing)
    {
        if (!disposed)
        {
            if (_disposing)
            {
                buffer = null;
                readableBuffer = null;
                readPosition = 0;
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
