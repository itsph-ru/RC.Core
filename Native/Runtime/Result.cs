﻿// =====================================//==============================================================//
// License="root\\LICENSE"              //   Copyright © Of Fire Twins Wesp 2015  <ls-micro@ya.ru>      //
// LicenseType="MIT"                    //                  Alise Wesp & Yuuki Wesp                     //
// =====================================//==============================================================//
using System;
using System.Runtime.InteropServices;
namespace Rc.Framework.Native.Runtime
{
    /// <summary>
    /// Result structure for COM methods.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Result : IEquatable<Result>
    {
        private int _code;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> struct.
        /// </summary>
        /// <param name="code">The HRESULT error code.</param>
        public Result(int code)
        {
            _code = code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> struct.
        /// </summary>
        /// <param name="code">The HRESULT error code.</param>
        public Result(uint code)
        {
            _code = unchecked((int)code);
        }

        /// <summary>
        /// Gets the HRESULT error code.
        /// </summary>
        /// <value>The HRESULT error code.</value>
        public int Code
        {
            get { return _code; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Result"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success
        {
            get { return Code >= 0; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Result"/> is failure.
        /// </summary>
        /// <value><c>true</c> if failure; otherwise, <c>false</c>.</value>
        public bool Failure
        {
            get { return Code < 0; }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Result"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator int (Result result)
        {
            return result.Code;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Result"/> to <see cref="System.UInt32"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator uint (Result result)
        {
            return unchecked((uint)result.Code);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Int32"/> to <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Result(int result)
        {
            return new Result(result);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.UInt32"/> to <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Result(uint result)
        {
            return new Result(result);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Result other)
        {
            return this.Code == other.Code;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Result))
                return false;
            return Equals((Result)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Code;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Result left, Result right)
        {
            return left.Code == right.Code;
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Result left, Result right)
        {
            return left.Code != right.Code;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "HRESULT = 0x{0:X}", _code);
        }

        /// <summary>
        /// Checks the error.
        /// </summary>
        public void CheckError()
        {
            if (_code < 0)
            {
                throw new COMException(this);
            }
        }

        /// <summary>
        /// Gets a <see cref="Result"/> from an <see cref="Exception"/>.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns>The associated result code</returns>
        public static Result GetResultFromException(Exception ex)
        {
            return new Result(Marshal.GetHRForException(ex));
        }

        /// <summary>
        /// Gets the result from win32 error.
        /// </summary>
        /// <param name="win32Error">The win32Error.</param>
        /// <returns>A HRESULT.</returns>
        public static Result GetResultFromWin32Error(int win32Error)
        {
            const int FACILITY_WIN32 = 7;
            return win32Error <= 0 ? win32Error : (int)((win32Error & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
        }

        /// <summary>
        /// Result code Ok
        /// </summary>
        /// <unmanaged>S_OK</unmanaged>
        public readonly static Result Ok = new Result(unchecked((int)0x00000000));

        /// <summary>
        /// Result code False
        /// </summary>
        /// <unmanaged>S_FALSE</unmanaged>
        public readonly static Result False = new Result(unchecked((int)0x00000001));

        /// <summary>
        /// Result code Abort
        /// </summary>
        /// <unmanaged>E_ABORT</unmanaged>
        public static readonly ResultDescriptor Abort = new ResultDescriptor(unchecked((int)0x80004004), "General", "E_ABORT", "Operation aborted");

        /// <summary>
        /// Result code AccessDenied
        /// </summary>
        /// <unmanaged>E_ACCESSDENIED</unmanaged>
        public static readonly ResultDescriptor AccessDenied = new ResultDescriptor(unchecked((int)0x80070005), "General", "E_ACCESSDENIED", "General access denied error");

        /// <summary>
        /// Result code Fail
        /// </summary>
        /// <unmanaged>E_FAIL</unmanaged>
        public static readonly ResultDescriptor Fail = new ResultDescriptor(unchecked((int)0x80004005), "General", "E_FAIL", "Unspecified error");

        /// <summary>
        /// Result code Handle
        /// </summary>
        /// <unmanaged>E_HANDLE</unmanaged>
        public static readonly ResultDescriptor Handle = new ResultDescriptor(unchecked((int)0x80070006), "General", "E_HANDLE", "Invalid handle");

        /// <summary>
        /// Result code invalid argument
        /// </summary>
        /// <unmanaged>E_INVALIDARG</unmanaged>
        public static readonly ResultDescriptor InvalidArg = new ResultDescriptor(unchecked((int)0x80070057), "General", "E_INVALIDARG", "Invalid Arguments");

        /// <summary>
        /// Result code no interface
        /// </summary>
        /// <unmanaged>E_NOINTERFACE</unmanaged>
        public static readonly ResultDescriptor NoInterface = new ResultDescriptor(unchecked((int)0x80004002), "General", "E_NOINTERFACE", "No such interface supported");

        /// <summary>
        /// Result code not implemented
        /// </summary>
        /// <unmanaged>E_NOTIMPL</unmanaged>
        public static readonly ResultDescriptor NotImplemented = new ResultDescriptor(unchecked((int)0x80004001), "General", "E_NOTIMPL", "Not implemented");

        /// <summary>
        /// Result code out of memory
        /// </summary>
        /// <unmanaged>E_OUTOFMEMORY</unmanaged>
        public static readonly ResultDescriptor OutOfMemory = new ResultDescriptor(unchecked((int)0x8007000E), "General", "E_OUTOFMEMORY", "Out of memory");

        /// <summary>
        /// Result code Invalid pointer
        /// </summary>
        /// <unmanaged>E_POINTER</unmanaged>
        public static readonly ResultDescriptor InvalidPointer = new ResultDescriptor(unchecked((int)0x80004003), "General", "E_POINTER", "Invalid pointer");

        /// <summary>
        /// Unexpected failure
        /// </summary>
        /// <unmanaged>E_UNEXPECTED</unmanaged>
        public static readonly ResultDescriptor UnexpectedFailure = new ResultDescriptor(unchecked((int)0x8000FFFF), "General", "E_UNEXPECTED", "Catastrophic failure");

        /// <summary>
        /// Result of a wait abandonned.
        /// </summary>
        /// <unmanaged>WAIT_ABANDONED</unmanaged>
        public static readonly ResultDescriptor WaitAbandoned = new ResultDescriptor(unchecked((int)0x00000080L), "General", "WAIT_ABANDONED", "WaitAbandoned");

        /// <summary>
        /// Result of a wait timeout.
        /// </summary>
        /// <unmanaged>WAIT_TIMEOUT</unmanaged>
        public static readonly ResultDescriptor WaitTimeout = new ResultDescriptor(unchecked((int)0x00000102L), "General", "WAIT_TIMEOUT", "WaitTimeout");
    }
}