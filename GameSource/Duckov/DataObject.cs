using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;

namespace System.Windows
{
	// Token: 0x02000425 RID: 1061
	public sealed class DataObject : IDataObject, IDataObject
	{
		// Token: 0x06002D6F RID: 11631 RVA: 0x00123DF1 File Offset: 0x001223F1
		public DataObject()
		{
			this._innerData = new DataObject.DataStore();
		}

		// Token: 0x06002D70 RID: 11632 RVA: 0x00123E04 File Offset: 0x00122404
		public DataObject(object data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			IDataObject dataObject = data as IDataObject;
			if (dataObject != null)
			{
				this._innerData = dataObject;
				return;
			}
			IDataObject dataObject2 = data as IDataObject;
			if (dataObject2 != null)
			{
				this._innerData = new DataObject.OleConverter(dataObject2);
				return;
			}
			this._innerData = new DataObject.DataStore();
			this.SetData(data);
		}

		// Token: 0x06002D71 RID: 11633 RVA: 0x00123E60 File Offset: 0x00122460
		public DataObject(string format, object data)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData = new DataObject.DataStore();
			this.SetData(format, data);
		}

		// Token: 0x06002D72 RID: 11634 RVA: 0x00123EC0 File Offset: 0x001224C0
		public DataObject(Type format, object data)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData = new DataObject.DataStore();
			this.SetData(format.FullName, data);
		}

		// Token: 0x06002D73 RID: 11635 RVA: 0x00123F10 File Offset: 0x00122510
		public DataObject(string format, object data, bool autoConvert)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData = new DataObject.DataStore();
			this.SetData(format, data, autoConvert);
		}

		// Token: 0x06002D74 RID: 11636 RVA: 0x00123F70 File Offset: 0x00122570
		internal DataObject(IDataObject data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData = data;
		}

		// Token: 0x06002D75 RID: 11637 RVA: 0x00123F8D File Offset: 0x0012258D
		internal DataObject(IDataObject data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData = new DataObject.OleConverter(data);
		}

		// Token: 0x06002D76 RID: 11638 RVA: 0x00123FAF File Offset: 0x001225AF
		public object GetData(string format, bool autoConvert)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			return this._innerData.GetData(format, autoConvert);
		}

		// Token: 0x06002D77 RID: 11639 RVA: 0x00123FE9 File Offset: 0x001225E9
		public object GetData(string format)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			return this.GetData(format, true);
		}

		// Token: 0x06002D78 RID: 11640 RVA: 0x0012401E File Offset: 0x0012261E
		public object GetData(Type format)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			return this.GetData(format.FullName);
		}

		// Token: 0x06002D79 RID: 11641 RVA: 0x00124040 File Offset: 0x00122640
		public bool GetDataPresent(Type format)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			return this.GetDataPresent(format.FullName);
		}

		// Token: 0x06002D7A RID: 11642 RVA: 0x00124062 File Offset: 0x00122662
		public bool GetDataPresent(string format, bool autoConvert)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			return this._innerData.GetDataPresent(format, autoConvert);
		}

		// Token: 0x06002D7B RID: 11643 RVA: 0x0012409C File Offset: 0x0012269C
		public bool GetDataPresent(string format)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			return this.GetDataPresent(format, true);
		}

		// Token: 0x06002D7C RID: 11644 RVA: 0x001240D1 File Offset: 0x001226D1
		public string[] GetFormats(bool autoConvert)
		{
			return this._innerData.GetFormats(autoConvert);
		}

		// Token: 0x06002D7D RID: 11645 RVA: 0x001240DF File Offset: 0x001226DF
		public string[] GetFormats()
		{
			return this.GetFormats(true);
		}

		// Token: 0x06002D7E RID: 11646 RVA: 0x001240E8 File Offset: 0x001226E8
		public void SetData(object data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData.SetData(data);
		}

		// Token: 0x06002D7F RID: 11647 RVA: 0x00124104 File Offset: 0x00122704
		public void SetData(string format, object data)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData.SetData(format, data);
		}

		// Token: 0x06002D80 RID: 11648 RVA: 0x00124157 File Offset: 0x00122757
		public void SetData(Type format, object data)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			this._innerData.SetData(format, data);
		}

		// Token: 0x06002D81 RID: 11649 RVA: 0x00124188 File Offset: 0x00122788
		[FriendAccessAllowed]
		public void SetData(string format, object data, bool autoConvert)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (format == string.Empty)
			{
				throw new ArgumentException(SR.Get("DataObject_EmptyFormatNotAllowed"));
			}
			this._innerData.SetData(format, data, autoConvert);
		}

		// Token: 0x06002D82 RID: 11650 RVA: 0x001241C3 File Offset: 0x001227C3
		public bool ContainsAudio()
		{
			return this.GetDataPresent(DataFormats.WaveAudio, false);
		}

		// Token: 0x06002D83 RID: 11651 RVA: 0x001241D1 File Offset: 0x001227D1
		public bool ContainsFileDropList()
		{
			return this.GetDataPresent(DataFormats.FileDrop, false);
		}

		// Token: 0x06002D84 RID: 11652 RVA: 0x001241DF File Offset: 0x001227DF
		public bool ContainsImage()
		{
			return this.GetDataPresent(DataFormats.Bitmap, false);
		}

		// Token: 0x06002D85 RID: 11653 RVA: 0x001241ED File Offset: 0x001227ED
		public bool ContainsText()
		{
			return this.ContainsText(TextDataFormat.UnicodeText);
		}

		// Token: 0x06002D86 RID: 11654 RVA: 0x001241F6 File Offset: 0x001227F6
		public bool ContainsText(TextDataFormat format)
		{
			if (!DataFormats.IsValidTextDataFormat(format))
			{
				throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
			}
			return this.GetDataPresent(DataFormats.ConvertToDataFormats(format), false);
		}

		// Token: 0x06002D87 RID: 11655 RVA: 0x00124223 File Offset: 0x00122823
		public Stream GetAudioStream()
		{
			return this.GetData(DataFormats.WaveAudio, false) as Stream;
		}

		// Token: 0x06002D88 RID: 11656 RVA: 0x00124238 File Offset: 0x00122838
		public StringCollection GetFileDropList()
		{
			StringCollection stringCollection = new StringCollection();
			string[] array = this.GetData(DataFormats.FileDrop, true) as string[];
			if (array != null)
			{
				stringCollection.AddRange(array);
			}
			return stringCollection;
		}

		// Token: 0x06002D89 RID: 11657 RVA: 0x00124268 File Offset: 0x00122868
		public BitmapSource GetImage()
		{
			return this.GetData(DataFormats.Bitmap, true) as BitmapSource;
		}

		// Token: 0x06002D8A RID: 11658 RVA: 0x0012427B File Offset: 0x0012287B
		public string GetText()
		{
			return this.GetText(TextDataFormat.UnicodeText);
		}

		// Token: 0x06002D8B RID: 11659 RVA: 0x00124284 File Offset: 0x00122884
		public string GetText(TextDataFormat format)
		{
			if (!DataFormats.IsValidTextDataFormat(format))
			{
				throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
			}
			string text = (string)this.GetData(DataFormats.ConvertToDataFormats(format), false);
			if (text != null)
			{
				return text;
			}
			return string.Empty;
		}

		// Token: 0x06002D8C RID: 11660 RVA: 0x001242CC File Offset: 0x001228CC
		public void SetAudio(byte[] audioBytes)
		{
			if (audioBytes == null)
			{
				throw new ArgumentNullException("audioBytes");
			}
			this.SetAudio(new MemoryStream(audioBytes));
		}

		// Token: 0x06002D8D RID: 11661 RVA: 0x001242E8 File Offset: 0x001228E8
		public void SetAudio(Stream audioStream)
		{
			if (audioStream == null)
			{
				throw new ArgumentNullException("audioStream");
			}
			this.SetData(DataFormats.WaveAudio, audioStream, false);
		}

		// Token: 0x06002D8E RID: 11662 RVA: 0x00124308 File Offset: 0x00122908
		public void SetFileDropList(StringCollection fileDropList)
		{
			if (fileDropList == null)
			{
				throw new ArgumentNullException("fileDropList");
			}
			if (fileDropList.Count == 0)
			{
				throw new ArgumentException(SR.Get("DataObject_FileDropListIsEmpty", new object[]
				{
					fileDropList
				}));
			}
			foreach (string path in fileDropList)
			{
				try
				{
					Path.GetFullPath(path);
				}
				catch (ArgumentException ex)
				{
					throw new ArgumentException(SR.Get("DataObject_FileDropListHasInvalidFileDropPath", new object[]
					{
						ex
					}));
				}
			}
			string[] array = new string[fileDropList.Count];
			fileDropList.CopyTo(array, 0);
			this.SetData(DataFormats.FileDrop, array, true);
		}

		// Token: 0x06002D8F RID: 11663 RVA: 0x001243D4 File Offset: 0x001229D4
		public void SetImage(BitmapSource image)
		{
			if (image == null)
			{
				throw new ArgumentNullException("image");
			}
			this.SetData(DataFormats.Bitmap, image, true);
		}

		// Token: 0x06002D90 RID: 11664 RVA: 0x001243F1 File Offset: 0x001229F1
		public void SetText(string textData)
		{
			if (textData == null)
			{
				throw new ArgumentNullException("textData");
			}
			this.SetText(textData, TextDataFormat.UnicodeText);
		}

		// Token: 0x06002D91 RID: 11665 RVA: 0x00124409 File Offset: 0x00122A09
		public void SetText(string textData, TextDataFormat format)
		{
			if (textData == null)
			{
				throw new ArgumentNullException("textData");
			}
			if (!DataFormats.IsValidTextDataFormat(format))
			{
				throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
			}
			this.SetData(DataFormats.ConvertToDataFormats(format), textData, false);
		}

		// Token: 0x06002D92 RID: 11666 RVA: 0x00124445 File Offset: 0x00122A45
		int IDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink pAdvSink, out int pdwConnection)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				return ((DataObject.OleConverter)this._innerData).OleDataObject.DAdvise(ref pFormatetc, advf, pAdvSink, out pdwConnection);
			}
			pdwConnection = 0;
			return -2147467263;
		}

		// Token: 0x06002D93 RID: 11667 RVA: 0x00124478 File Offset: 0x00122A78
		void IDataObject.DUnadvise(int dwConnection)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				((DataObject.OleConverter)this._innerData).OleDataObject.DUnadvise(dwConnection);
				return;
			}
			Marshal.ThrowExceptionForHR(-2147467263);
		}

		// Token: 0x06002D94 RID: 11668 RVA: 0x001244A8 File Offset: 0x00122AA8
		int IDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				return ((DataObject.OleConverter)this._innerData).OleDataObject.EnumDAdvise(out enumAdvise);
			}
			enumAdvise = null;
			return -2147221501;
		}

		// Token: 0x06002D95 RID: 11669 RVA: 0x001244D8 File Offset: 0x00122AD8
		IEnumFORMATETC IDataObject.EnumFormatEtc(DATADIR dwDirection)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				return ((DataObject.OleConverter)this._innerData).OleDataObject.EnumFormatEtc(dwDirection);
			}
			if (dwDirection == DATADIR.DATADIR_GET)
			{
				return new DataObject.FormatEnumerator(this);
			}
			throw new ExternalException(SR.Get("DataObject_NotImplementedEnumFormatEtc", new object[]
			{
				dwDirection
			}), -2147467263);
		}

		// Token: 0x06002D96 RID: 11670 RVA: 0x00124538 File Offset: 0x00122B38
		int IDataObject.GetCanonicalFormatEtc(ref FORMATETC pformatetcIn, out FORMATETC pformatetcOut)
		{
			pformatetcOut = default(FORMATETC);
			pformatetcOut = pformatetcIn;
			pformatetcOut.ptd = IntPtr.Zero;
			if (pformatetcIn.lindex != -1)
			{
				return -2147221400;
			}
			if (this._innerData is DataObject.OleConverter)
			{
				return ((DataObject.OleConverter)this._innerData).OleDataObject.GetCanonicalFormatEtc(ref pformatetcIn, out pformatetcOut);
			}
			return 262448;
		}

		// Token: 0x06002D97 RID: 11671 RVA: 0x0012459C File Offset: 0x00122B9C
		void IDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				((DataObject.OleConverter)this._innerData).OleDataObject.GetData(ref formatetc, out medium);
				return;
			}
			int num = -2147221399;
			medium = default(STGMEDIUM);
			if (this.GetTymedUseable(formatetc.tymed))
			{
				if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != TYMED.TYMED_NULL)
				{
					medium.tymed = TYMED.TYMED_HGLOBAL;
					medium.unionmember = DataObject.Win32GlobalAlloc(8258, (IntPtr)1);
					num = this.OleGetDataUnrestricted(ref formatetc, ref medium, false);
					if (NativeMethods.Failed(num))
					{
						DataObject.Win32GlobalFree(new HandleRef(this, medium.unionmember));
					}
				}
				else if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != TYMED.TYMED_NULL)
				{
					medium.tymed = TYMED.TYMED_ISTREAM;
					IStream o = null;
					num = DataObject.Win32CreateStreamOnHGlobal(IntPtr.Zero, true, ref o);
					if (NativeMethods.Succeeded(num))
					{
						medium.unionmember = Marshal.GetComInterfaceForObject(o, typeof(IStream));
						Marshal.ReleaseComObject(o);
						num = this.OleGetDataUnrestricted(ref formatetc, ref medium, false);
						if (NativeMethods.Failed(num))
						{
							Marshal.Release(medium.unionmember);
						}
					}
				}
				else
				{
					medium.tymed = formatetc.tymed;
					num = this.OleGetDataUnrestricted(ref formatetc, ref medium, false);
				}
			}
			if (NativeMethods.Failed(num))
			{
				medium.unionmember = IntPtr.Zero;
				Marshal.ThrowExceptionForHR(num);
			}
		}

		// Token: 0x06002D98 RID: 11672 RVA: 0x001246D0 File Offset: 0x00122CD0
		void IDataObject.GetDataHere(ref FORMATETC formatetc, ref STGMEDIUM medium)
		{
			if (medium.tymed != TYMED.TYMED_ISTORAGE && medium.tymed != TYMED.TYMED_ISTREAM && medium.tymed != TYMED.TYMED_HGLOBAL && medium.tymed != TYMED.TYMED_FILE)
			{
				Marshal.ThrowExceptionForHR(-2147221399);
			}
			int num = this.OleGetDataUnrestricted(ref formatetc, ref medium, true);
			if (NativeMethods.Failed(num))
			{
				Marshal.ThrowExceptionForHR(num);
			}
		}

		// Token: 0x06002D99 RID: 11673 RVA: 0x00124724 File Offset: 0x00122D24
		int IDataObject.QueryGetData(ref FORMATETC formatetc)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				return ((DataObject.OleConverter)this._innerData).OleDataObject.QueryGetData(ref formatetc);
			}
			if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
			{
				return -2147221397;
			}
			if (!this.GetTymedUseable(formatetc.tymed))
			{
				return -2147221399;
			}
			if (formatetc.cfFormat == 0)
			{
				return 1;
			}
			if (!this.GetDataPresent(DataFormats.GetDataFormat((int)formatetc.cfFormat).Name))
			{
				return -2147221404;
			}
			return 0;
		}

		// Token: 0x06002D9A RID: 11674 RVA: 0x001247A1 File Offset: 0x00122DA1
		void IDataObject.SetData(ref FORMATETC pFormatetcIn, ref STGMEDIUM pmedium, bool fRelease)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				((DataObject.OleConverter)this._innerData).OleDataObject.SetData(ref pFormatetcIn, ref pmedium, fRelease);
				return;
			}
			Marshal.ThrowExceptionForHR(-2147467263);
		}

		// Token: 0x06002D9B RID: 11675 RVA: 0x001247D3 File Offset: 0x00122DD3
		public static void AddCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
		{
			UIElement.AddHandler(element, DataObject.CopyingEvent, handler);
		}

		// Token: 0x06002D9C RID: 11676 RVA: 0x001247E1 File Offset: 0x00122DE1
		public static void RemoveCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
		{
			UIElement.RemoveHandler(element, DataObject.CopyingEvent, handler);
		}

		// Token: 0x06002D9D RID: 11677 RVA: 0x001247EF File Offset: 0x00122DEF
		public static void AddPastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
		{
			UIElement.AddHandler(element, DataObject.PastingEvent, handler);
		}

		// Token: 0x06002D9E RID: 11678 RVA: 0x001247FD File Offset: 0x00122DFD
		public static void RemovePastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
		{
			UIElement.RemoveHandler(element, DataObject.PastingEvent, handler);
		}

		// Token: 0x06002D9F RID: 11679 RVA: 0x0012480B File Offset: 0x00122E0B
		public static void AddSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
		{
			UIElement.AddHandler(element, DataObject.SettingDataEvent, handler);
		}

		// Token: 0x06002DA0 RID: 11680 RVA: 0x00124819 File Offset: 0x00122E19
		public static void RemoveSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
		{
			UIElement.RemoveHandler(element, DataObject.SettingDataEvent, handler);
		}

		// Token: 0x06002DA1 RID: 11681 RVA: 0x00124828 File Offset: 0x00122E28
		internal static IntPtr Win32GlobalAlloc(int flags, IntPtr bytes)
		{
			IntPtr intPtr = UnsafeNativeMethods.GlobalAlloc(flags, bytes);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception(lastWin32Error);
			}
			return intPtr;
		}

		// Token: 0x06002DA2 RID: 11682 RVA: 0x00124858 File Offset: 0x00122E58
		private static int Win32CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, ref IStream istream)
		{
			int num = UnsafeNativeMethods.CreateStreamOnHGlobal(hGlobal, fDeleteOnRelease, ref istream);
			if (NativeMethods.Failed(num))
			{
				Marshal.ThrowExceptionForHR(num);
			}
			return num;
		}

		// Token: 0x06002DA3 RID: 11683 RVA: 0x00124880 File Offset: 0x00122E80
		internal static void Win32GlobalFree(HandleRef handle)
		{
			IntPtr value = UnsafeNativeMethods.GlobalFree(handle);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (value != IntPtr.Zero)
			{
				throw new Win32Exception(lastWin32Error);
			}
		}

		// Token: 0x06002DA4 RID: 11684 RVA: 0x001248AC File Offset: 0x00122EAC
		internal static IntPtr Win32GlobalReAlloc(HandleRef handle, IntPtr bytes, int flags)
		{
			IntPtr intPtr = UnsafeNativeMethods.GlobalReAlloc(handle, bytes, flags);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception(lastWin32Error);
			}
			return intPtr;
		}

		// Token: 0x06002DA5 RID: 11685 RVA: 0x001248DC File Offset: 0x00122EDC
		internal static IntPtr Win32GlobalLock(HandleRef handle)
		{
			IntPtr intPtr = UnsafeNativeMethods.GlobalLock(handle);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception(lastWin32Error);
			}
			return intPtr;
		}

		// Token: 0x06002DA6 RID: 11686 RVA: 0x0012490C File Offset: 0x00122F0C
		internal static void Win32GlobalUnlock(HandleRef handle)
		{
			bool flag = UnsafeNativeMethods.GlobalUnlock(handle);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (!flag && lastWin32Error != 0)
			{
				throw new Win32Exception(lastWin32Error);
			}
		}

		// Token: 0x06002DA7 RID: 11687 RVA: 0x00124934 File Offset: 0x00122F34
		internal static IntPtr Win32GlobalSize(HandleRef handle)
		{
			IntPtr intPtr = UnsafeNativeMethods.GlobalSize(handle);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception(lastWin32Error);
			}
			return intPtr;
		}

		// Token: 0x06002DA8 RID: 11688 RVA: 0x00124961 File Offset: 0x00122F61
		internal static IntPtr Win32SelectObject(HandleRef handleDC, IntPtr handleObject)
		{
			IntPtr intPtr = UnsafeNativeMethods.SelectObject(handleDC, handleObject);
			if (intPtr == IntPtr.Zero)
			{
				throw new Win32Exception();
			}
			return intPtr;
		}

		// Token: 0x06002DA9 RID: 11689 RVA: 0x0012497D File Offset: 0x00122F7D
		internal static void Win32DeleteObject(HandleRef handleDC)
		{
			UnsafeNativeMethods.DeleteObject(handleDC);
		}

		// Token: 0x06002DAA RID: 11690 RVA: 0x00124985 File Offset: 0x00122F85
		internal static IntPtr Win32GetDC(HandleRef handleDC)
		{
			return UnsafeNativeMethods.GetDC(handleDC);
		}

		// Token: 0x06002DAB RID: 11691 RVA: 0x0012498D File Offset: 0x00122F8D
		internal static IntPtr Win32CreateCompatibleDC(HandleRef handleDC)
		{
			return UnsafeNativeMethods.CreateCompatibleDC(handleDC);
		}

		// Token: 0x06002DAC RID: 11692 RVA: 0x00124995 File Offset: 0x00122F95
		internal static IntPtr Win32CreateCompatibleBitmap(HandleRef handleDC, int width, int height)
		{
			return UnsafeNativeMethods.CreateCompatibleBitmap(handleDC, width, height);
		}

		// Token: 0x06002DAD RID: 11693 RVA: 0x0012499F File Offset: 0x00122F9F
		internal static void Win32DeleteDC(HandleRef handleDC)
		{
			UnsafeNativeMethods.DeleteDC(handleDC);
		}

		// Token: 0x06002DAE RID: 11694 RVA: 0x001249A7 File Offset: 0x00122FA7
		private static void Win32ReleaseDC(HandleRef handleHWND, HandleRef handleDC)
		{
			UnsafeNativeMethods.ReleaseDC(handleHWND, handleDC);
		}

		// Token: 0x06002DAF RID: 11695 RVA: 0x001249B4 File Offset: 0x00122FB4
		internal static void Win32BitBlt(HandleRef handledestination, int width, int height, HandleRef handleSource, int operationCode)
		{
			if (!UnsafeNativeMethods.BitBlt(handledestination, 0, 0, width, height, handleSource, 0, 0, operationCode))
			{
				throw new Win32Exception();
			}
		}

		// Token: 0x06002DB0 RID: 11696 RVA: 0x001249D8 File Offset: 0x00122FD8
		internal static int Win32WideCharToMultiByte(string wideString, int wideChars, byte[] bytes, int byteCount)
		{
			int num = UnsafeNativeMethods.WideCharToMultiByte(0, 0, wideString, wideChars, bytes, byteCount, IntPtr.Zero, IntPtr.Zero);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (num == 0)
			{
				throw new Win32Exception(lastWin32Error);
			}
			return num;
		}

		// Token: 0x06002DB1 RID: 11697 RVA: 0x00124A0C File Offset: 0x0012300C
		internal static string[] GetMappedFormats(string format)
		{
			if (format == null)
			{
				return null;
			}
			if (DataObject.IsFormatEqual(format, DataFormats.Text) || DataObject.IsFormatEqual(format, DataFormats.UnicodeText) || DataObject.IsFormatEqual(format, DataFormats.StringFormat))
			{
				return new string[]
				{
					DataFormats.Text,
					DataFormats.UnicodeText,
					DataFormats.StringFormat
				};
			}
			if (DataObject.IsFormatEqual(format, DataFormats.FileDrop) || DataObject.IsFormatEqual(format, DataFormats.FileName) || DataObject.IsFormatEqual(format, DataFormats.FileNameW))
			{
				return new string[]
				{
					DataFormats.FileDrop,
					DataFormats.FileNameW,
					DataFormats.FileName
				};
			}
			if (DataObject.IsFormatEqual(format, DataFormats.Bitmap) || DataObject.IsFormatEqual(format, "System.Windows.Media.Imaging.BitmapSource") || DataObject.IsFormatEqual(format, "System.Drawing.Bitmap"))
			{
				return new string[]
				{
					DataFormats.Bitmap,
					"System.Drawing.Bitmap",
					"System.Windows.Media.Imaging.BitmapSource"
				};
			}
			if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile) || DataObject.IsFormatEqual(format, "System.Drawing.Imaging.Metafile"))
			{
				return new string[]
				{
					DataFormats.EnhancedMetafile,
					"System.Drawing.Imaging.Metafile"
				};
			}
			return new string[]
			{
				format
			};
		}

		// Token: 0x06002DB2 RID: 11698 RVA: 0x00124B2B File Offset: 0x0012312B
		private int OleGetDataUnrestricted(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
		{
			if (this._innerData is DataObject.OleConverter)
			{
				((DataObject.OleConverter)this._innerData).OleDataObject.GetDataHere(ref formatetc, ref medium);
				return 0;
			}
			return this.GetDataIntoOleStructs(ref formatetc, ref medium, doNotReallocate);
		}

		// Token: 0x06002DB3 RID: 11699 RVA: 0x00124B5C File Offset: 0x0012315C
		private static string[] GetDistinctStrings(string[] formats)
		{
			ArrayList arrayList = new ArrayList();
			foreach (string text in formats)
			{
				if (!arrayList.Contains(text))
				{
					arrayList.Add(text);
				}
			}
			string[] array = new string[arrayList.Count];
			arrayList.CopyTo(array, 0);
			return array;
		}

		// Token: 0x06002DB4 RID: 11700 RVA: 0x00124BA8 File Offset: 0x001231A8
		private bool GetTymedUseable(TYMED tymed)
		{
			for (int i = 0; i < DataObject.ALLOWED_TYMEDS.Length; i++)
			{
				if ((tymed & DataObject.ALLOWED_TYMEDS[i]) != TYMED.TYMED_NULL)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002DB5 RID: 11701 RVA: 0x00124BD8 File Offset: 0x001231D8
		private IntPtr GetCompatibleBitmap(object data)
		{
			int width;
			int height;
			IntPtr hbitmap = SystemDrawingHelper.GetHBitmap(data, out width, out height);
			if (hbitmap == IntPtr.Zero)
			{
				return IntPtr.Zero;
			}
			IntPtr intPtr;
			try
			{
				IntPtr handle = DataObject.Win32GetDC(new HandleRef(this, IntPtr.Zero));
				IntPtr handle2 = DataObject.Win32CreateCompatibleDC(new HandleRef(this, handle));
				IntPtr handleObject = DataObject.Win32SelectObject(new HandleRef(this, handle2), hbitmap);
				IntPtr handle3 = DataObject.Win32CreateCompatibleDC(new HandleRef(this, handle));
				intPtr = DataObject.Win32CreateCompatibleBitmap(new HandleRef(this, handle), width, height);
				IntPtr handleObject2 = DataObject.Win32SelectObject(new HandleRef(this, handle3), intPtr);
				try
				{
					DataObject.Win32BitBlt(new HandleRef(this, handle3), width, height, new HandleRef(null, handle2), 13369376);
				}
				finally
				{
					DataObject.Win32SelectObject(new HandleRef(this, handle2), handleObject);
					DataObject.Win32SelectObject(new HandleRef(this, handle3), handleObject2);
					DataObject.Win32DeleteDC(new HandleRef(this, handle2));
					DataObject.Win32DeleteDC(new HandleRef(this, handle3));
					DataObject.Win32ReleaseDC(new HandleRef(this, IntPtr.Zero), new HandleRef(this, handle));
				}
			}
			finally
			{
				DataObject.Win32DeleteObject(new HandleRef(this, hbitmap));
			}
			return intPtr;
		}

		// Token: 0x06002DB6 RID: 11702 RVA: 0x00124CFC File Offset: 0x001232FC
		private IntPtr GetEnhancedMetafileHandle(string format, object data)
		{
			IntPtr intPtr = IntPtr.Zero;
			if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile))
			{
				if (SystemDrawingHelper.IsMetafile(data))
				{
					intPtr = SystemDrawingHelper.GetHandleFromMetafile(data);
				}
				else if (data is MemoryStream)
				{
					MemoryStream memoryStream = data as MemoryStream;
					if (memoryStream != null)
					{
						byte[] buffer = memoryStream.GetBuffer();
						if (buffer != null && buffer.Length != 0)
						{
							intPtr = NativeMethods.SetEnhMetaFileBits((uint)buffer.Length, buffer);
							int lastWin32Error = Marshal.GetLastWin32Error();
							if (intPtr == IntPtr.Zero)
							{
								throw new Win32Exception(lastWin32Error);
							}
						}
					}
				}
			}
			return intPtr;
		}

		// Token: 0x06002DB7 RID: 11703 RVA: 0x00124D74 File Offset: 0x00123374
		private int GetDataIntoOleStructs(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
		{
			int result = -2147221399;
			if (this.GetTymedUseable(formatetc.tymed) && this.GetTymedUseable(medium.tymed))
			{
				string name = DataFormats.GetDataFormat((int)formatetc.cfFormat).Name;
				result = -2147221404;
				if (this.GetDataPresent(name))
				{
					object data = this.GetData(name);
					result = -2147221399;
					if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != TYMED.TYMED_NULL)
					{
						result = this.GetDataIntoOleStructsByTypeMedimHGlobal(name, data, ref medium, doNotReallocate);
					}
					else if ((formatetc.tymed & TYMED.TYMED_GDI) != TYMED.TYMED_NULL)
					{
						result = this.GetDataIntoOleStructsByTypeMediumGDI(name, data, ref medium);
					}
					else if ((formatetc.tymed & TYMED.TYMED_ENHMF) != TYMED.TYMED_NULL)
					{
						result = this.GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(name, data, ref medium);
					}
					else if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != TYMED.TYMED_NULL)
					{
						result = this.GetDataIntoOleStructsByTypeMedimIStream(name, data, ref medium);
					}
				}
			}
			return result;
		}

		// Token: 0x06002DB8 RID: 11704 RVA: 0x00124E34 File Offset: 0x00123434
		private int GetDataIntoOleStructsByTypeMedimHGlobal(string format, object data, ref STGMEDIUM medium, bool doNotReallocate)
		{
			int num;
			if (data is Stream)
			{
				num = this.SaveStreamToHandle(medium.unionmember, (Stream)data, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.Html) || DataObject.IsFormatEqual(format, DataFormats.Xaml))
			{
				num = this.SaveStringToHandleAsUtf8(medium.unionmember, data.ToString(), doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.Text) || DataObject.IsFormatEqual(format, DataFormats.Rtf) || DataObject.IsFormatEqual(format, DataFormats.OemText) || DataObject.IsFormatEqual(format, DataFormats.CommaSeparatedValue))
			{
				num = this.SaveStringToHandle(medium.unionmember, data.ToString(), false, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.UnicodeText) || DataObject.IsFormatEqual(format, DataFormats.ApplicationTrust))
			{
				num = this.SaveStringToHandle(medium.unionmember, data.ToString(), true, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.FileDrop))
			{
				num = this.SaveFileListToHandle(medium.unionmember, (string[])data, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.FileName))
			{
				string[] array = (string[])data;
				num = this.SaveStringToHandle(medium.unionmember, array[0], false, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.FileNameW))
			{
				string[] array2 = (string[])data;
				num = this.SaveStringToHandle(medium.unionmember, array2[0], true, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.Dib) && SystemDrawingHelper.IsImage(data))
			{
				num = -2147221399;
			}
			else if (DataObject.IsFormatEqual(format, typeof(BitmapSource).FullName))
			{
				num = this.SaveSystemBitmapSourceToHandle(medium.unionmember, data, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, "System.Drawing.Bitmap"))
			{
				num = this.SaveSystemDrawingBitmapToHandle(medium.unionmember, data, doNotReallocate);
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile) || SystemDrawingHelper.IsMetafile(data))
			{
				num = -2147221399;
			}
			else if (DataObject.IsFormatEqual(format, DataFormats.Serializable) || data is ISerializable || (data != null && data.GetType().IsSerializable))
			{
				num = this.SaveObjectToHandle(medium.unionmember, data, doNotReallocate);
			}
			else
			{
				num = -2147221399;
			}
			if (num == 0)
			{
				medium.tymed = TYMED.TYMED_HGLOBAL;
			}
			return num;
		}

		// Token: 0x06002DB9 RID: 11705 RVA: 0x00125070 File Offset: 0x00123670
		private int GetDataIntoOleStructsByTypeMedimIStream(string format, object data, ref STGMEDIUM medium)
		{
			IStream stream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
			if (stream == null)
			{
				return -2147024809;
			}
			int num = -2147467259;
			try
			{
				if (format == StrokeCollection.InkSerializedFormat)
				{
					Stream stream2 = data as Stream;
					if (stream2 != null)
					{
						IntPtr intPtr = (IntPtr)stream2.Length;
						byte[] array = new byte[NativeMethods.IntPtrToInt32(intPtr)];
						stream2.Position = 0L;
						stream2.Read(array, 0, NativeMethods.IntPtrToInt32(intPtr));
						stream.Write(array, NativeMethods.IntPtrToInt32(intPtr), IntPtr.Zero);
						num = 0;
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(stream);
			}
			if (NativeMethods.Succeeded(num))
			{
				medium.tymed = TYMED.TYMED_ISTREAM;
			}
			return num;
		}

		// Token: 0x06002DBA RID: 11706 RVA: 0x00125124 File Offset: 0x00123724
		private int GetDataIntoOleStructsByTypeMediumGDI(string format, object data, ref STGMEDIUM medium)
		{
			int result = -2147467259;
			if (DataObject.IsFormatEqual(format, DataFormats.Bitmap) && (SystemDrawingHelper.IsBitmap(data) || DataObject.IsDataSystemBitmapSource(data)))
			{
				IntPtr compatibleBitmap = this.GetCompatibleBitmap(data);
				if (compatibleBitmap != IntPtr.Zero)
				{
					medium.tymed = TYMED.TYMED_GDI;
					medium.unionmember = compatibleBitmap;
					result = 0;
				}
			}
			else
			{
				result = -2147221399;
			}
			return result;
		}

		// Token: 0x06002DBB RID: 11707 RVA: 0x00125184 File Offset: 0x00123784
		private int GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(string format, object data, ref STGMEDIUM medium)
		{
			int result = -2147467259;
			if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile))
			{
				IntPtr enhancedMetafileHandle = this.GetEnhancedMetafileHandle(format, data);
				if (enhancedMetafileHandle != IntPtr.Zero)
				{
					medium.tymed = TYMED.TYMED_ENHMF;
					medium.unionmember = enhancedMetafileHandle;
					result = 0;
				}
			}
			else
			{
				result = -2147221399;
			}
			return result;
		}

		// Token: 0x06002DBC RID: 11708 RVA: 0x001251D4 File Offset: 0x001237D4
		private int SaveObjectToHandle(IntPtr handle, object data, bool doNotReallocate)
		{
			Stream stream2;
			Stream stream = stream2 = new MemoryStream();
			int result;
			try
			{
				BinaryWriter binaryWriter2;
				BinaryWriter binaryWriter = binaryWriter2 = new BinaryWriter(stream);
				try
				{
					binaryWriter.Write(DataObject._serializedObjectID);
					new BinaryFormatter().Serialize(stream, data);
					result = this.SaveStreamToHandle(handle, stream, doNotReallocate);
				}
				finally
				{
					if (binaryWriter2 != null)
					{
						((IDisposable)binaryWriter2).Dispose();
					}
				}
			}
			finally
			{
				if (stream2 != null)
				{
					((IDisposable)stream2).Dispose();
				}
			}
			return result;
		}

		// Token: 0x06002DBD RID: 11709 RVA: 0x00125248 File Offset: 0x00123848
		private int SaveStreamToHandle(IntPtr handle, Stream stream, bool doNotReallocate)
		{
			if (handle == IntPtr.Zero)
			{
				return -2147024809;
			}
			IntPtr intPtr = (IntPtr)stream.Length;
			int num = this.EnsureMemoryCapacity(ref handle, NativeMethods.IntPtrToInt32(intPtr), doNotReallocate);
			if (NativeMethods.Failed(num))
			{
				return num;
			}
			IntPtr destination = DataObject.Win32GlobalLock(new HandleRef(this, handle));
			try
			{
				byte[] array = new byte[NativeMethods.IntPtrToInt32(intPtr)];
				stream.Position = 0L;
				stream.Read(array, 0, NativeMethods.IntPtrToInt32(intPtr));
				Marshal.Copy(array, 0, destination, NativeMethods.IntPtrToInt32(intPtr));
			}
			finally
			{
				DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
			}
			return 0;
		}

		// Token: 0x06002DBE RID: 11710 RVA: 0x001252EC File Offset: 0x001238EC
		private int SaveSystemBitmapSourceToHandle(IntPtr handle, object data, bool doNotReallocate)
		{
			BitmapSource bitmapSource = null;
			if (DataObject.IsDataSystemBitmapSource(data))
			{
				bitmapSource = (BitmapSource)data;
			}
			else if (SystemDrawingHelper.IsBitmap(data))
			{
				IntPtr hbitmapFromBitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
				bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hbitmapFromBitmap, IntPtr.Zero, Int32Rect.Empty, null);
				DataObject.Win32DeleteObject(new HandleRef(this, hbitmapFromBitmap));
			}
			Invariant.Assert(bitmapSource != null);
			BmpBitmapEncoder bmpBitmapEncoder = new BmpBitmapEncoder();
			bmpBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			Stream stream = new MemoryStream();
			bmpBitmapEncoder.Save(stream);
			return this.SaveStreamToHandle(handle, stream, doNotReallocate);
		}

		// Token: 0x06002DBF RID: 11711 RVA: 0x00125370 File Offset: 0x00123970
		private int SaveSystemDrawingBitmapToHandle(IntPtr handle, object data, bool doNotReallocate)
		{
			object bitmap = SystemDrawingHelper.GetBitmap(data);
			Invariant.Assert(bitmap != null);
			return this.SaveObjectToHandle(handle, bitmap, doNotReallocate);
		}

		// Token: 0x06002DC0 RID: 11712 RVA: 0x00125398 File Offset: 0x00123998
		private int SaveFileListToHandle(IntPtr handle, string[] files, bool doNotReallocate)
		{
			if (files == null || files.Length < 1)
			{
				return 0;
			}
			if (handle == IntPtr.Zero)
			{
				return -2147024809;
			}
			if (Marshal.SystemDefaultCharSize == 1)
			{
				Invariant.Assert(false, "Expected the system default char size to be 2 for Unicode systems.");
				return -2147024809;
			}
			IntPtr intPtr = IntPtr.Zero;
			int num = 20;
			int num2 = num;
			for (int i = 0; i < files.Length; i++)
			{
				num2 += (files[i].Length + 1) * 2;
			}
			num2 += 2;
			int num3 = this.EnsureMemoryCapacity(ref handle, num2, doNotReallocate);
			if (NativeMethods.Failed(num3))
			{
				return num3;
			}
			IntPtr intPtr2 = DataObject.Win32GlobalLock(new HandleRef(this, handle));
			try
			{
				intPtr = intPtr2;
				int[] array = new int[5];
				array[0] = num;
				int[] array2 = array;
				array2[4] = -1;
				Marshal.Copy(array2, 0, intPtr, array2.Length);
				intPtr = (IntPtr)((long)intPtr + (long)num);
				for (int j = 0; j < files.Length; j++)
				{
					UnsafeNativeMethods.CopyMemoryW(intPtr, files[j], files[j].Length * 2);
					intPtr = (IntPtr)((long)intPtr + (long)(files[j].Length * 2));
					Marshal.Copy(new char[1], 0, intPtr, 1);
					intPtr = (IntPtr)((long)intPtr + 2L);
				}
				Marshal.Copy(new char[1], 0, intPtr, 1);
			}
			finally
			{
				DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
			}
			return 0;
		}

		// Token: 0x06002DC1 RID: 11713 RVA: 0x001254EC File Offset: 0x00123AEC
		private int SaveStringToHandle(IntPtr handle, string str, bool unicode, bool doNotReallocate)
		{
			if (handle == IntPtr.Zero)
			{
				return -2147024809;
			}
			if (unicode)
			{
				int minimumByteCount = str.Length * 2 + 2;
				int num = this.EnsureMemoryCapacity(ref handle, minimumByteCount, doNotReallocate);
				if (NativeMethods.Failed(num))
				{
					return num;
				}
				IntPtr intPtr = DataObject.Win32GlobalLock(new HandleRef(this, handle));
				try
				{
					char[] array = str.ToCharArray(0, str.Length);
					UnsafeNativeMethods.CopyMemoryW(intPtr, array, array.Length * 2);
					Marshal.Copy(new char[1], 0, (IntPtr)((long)intPtr + (long)array.Length * 2L), 1);
					return 0;
				}
				finally
				{
					DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
				}
			}
			int num2;
			if (str.Length > 0)
			{
				num2 = DataObject.Win32WideCharToMultiByte(str, str.Length, null, 0);
			}
			else
			{
				num2 = 0;
			}
			byte[] array2 = new byte[num2];
			if (num2 > 0)
			{
				DataObject.Win32WideCharToMultiByte(str, str.Length, array2, array2.Length);
			}
			int num3 = this.EnsureMemoryCapacity(ref handle, num2 + 1, doNotReallocate);
			if (NativeMethods.Failed(num3))
			{
				return num3;
			}
			IntPtr intPtr2 = DataObject.Win32GlobalLock(new HandleRef(this, handle));
			try
			{
				UnsafeNativeMethods.CopyMemory(intPtr2, array2, num2);
				Marshal.Copy(new byte[1], 0, (IntPtr)((long)intPtr2 + (long)num2), 1);
			}
			finally
			{
				DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
			}
			return 0;
		}

		// Token: 0x06002DC2 RID: 11714 RVA: 0x00125648 File Offset: 0x00123C48
		private int SaveStringToHandleAsUtf8(IntPtr handle, string str, bool doNotReallocate)
		{
			if (handle == IntPtr.Zero)
			{
				return -2147024809;
			}
			UTF8Encoding utf8Encoding = new UTF8Encoding();
			int byteCount = utf8Encoding.GetByteCount(str);
			byte[] bytes = utf8Encoding.GetBytes(str);
			int num = this.EnsureMemoryCapacity(ref handle, byteCount + 1, doNotReallocate);
			if (NativeMethods.Failed(num))
			{
				return num;
			}
			IntPtr intPtr = DataObject.Win32GlobalLock(new HandleRef(this, handle));
			try
			{
				UnsafeNativeMethods.CopyMemory(intPtr, bytes, byteCount);
				Marshal.Copy(new byte[1], 0, (IntPtr)((long)intPtr + (long)byteCount), 1);
			}
			finally
			{
				DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
			}
			return 0;
		}

		// Token: 0x06002DC3 RID: 11715 RVA: 0x001256E4 File Offset: 0x00123CE4
		private static bool IsDataSystemBitmapSource(object data)
		{
			return data is BitmapSource;
		}

		// Token: 0x06002DC4 RID: 11716 RVA: 0x001256F1 File Offset: 0x00123CF1
		private static bool IsFormatAndDataSerializable(string format, object data)
		{
			return DataObject.IsFormatEqual(format, DataFormats.Serializable) || data is ISerializable || (data != null && data.GetType().IsSerializable);
		}

		// Token: 0x06002DC5 RID: 11717 RVA: 0x0012571A File Offset: 0x00123D1A
		private static bool IsFormatEqual(string format1, string format2)
		{
			return string.CompareOrdinal(format1, format2) == 0;
		}

		// Token: 0x06002DC6 RID: 11718 RVA: 0x00125728 File Offset: 0x00123D28
		private int EnsureMemoryCapacity(ref IntPtr handle, int minimumByteCount, bool doNotReallocate)
		{
			int result = 0;
			if (doNotReallocate)
			{
				if (NativeMethods.IntPtrToInt32(DataObject.Win32GlobalSize(new HandleRef(this, handle))) < minimumByteCount)
				{
					handle = IntPtr.Zero;
					result = -2147286928;
				}
			}
			else
			{
				handle = DataObject.Win32GlobalReAlloc(new HandleRef(this, handle), (IntPtr)minimumByteCount, 8258);
				if (handle == IntPtr.Zero)
				{
					result = -2147024882;
				}
			}
			return result;
		}

		// Token: 0x06002DC7 RID: 11719 RVA: 0x00125790 File Offset: 0x00123D90
		private static object EnsureBitmapDataFromFormat(string format, bool autoConvert, object data)
		{
			object result = data;
			if (DataObject.IsDataSystemBitmapSource(data) && DataObject.IsFormatEqual(format, "System.Drawing.Bitmap"))
			{
				if (autoConvert)
				{
					result = SystemDrawingHelper.GetBitmap(data);
				}
				else
				{
					result = null;
				}
			}
			else if (SystemDrawingHelper.IsBitmap(data) && (DataObject.IsFormatEqual(format, DataFormats.Bitmap) || DataObject.IsFormatEqual(format, "System.Windows.Media.Imaging.BitmapSource")))
			{
				if (autoConvert)
				{
					IntPtr hbitmapFromBitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
					result = Imaging.CreateBitmapSourceFromHBitmap(hbitmapFromBitmap, IntPtr.Zero, Int32Rect.Empty, null);
					DataObject.Win32DeleteObject(new HandleRef(null, hbitmapFromBitmap));
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x06002DC8 RID: 11720 RVA: 0x00125814 File Offset: 0x00123E14
		// Note: this type is marked as 'beforefieldinit'.
		static DataObject()
		{
			TYMED[] array = new TYMED[5];
			RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.962C732E4EFE979C16B0229A7A7F6E81EC12A8FD55D25E4E7567E185D4A70D20).FieldHandle);
			DataObject.ALLOWED_TYMEDS = array;
			DataObject._serializedObjectID = new Guid(4255033238U, 15123, 17264, 166, 121, 86, 16, 107, 178, 136, 251).ToByteArray();
		}

		// Token: 0x04001E73 RID: 7795
		public static readonly RoutedEvent CopyingEvent = EventManager.RegisterRoutedEvent("Copying", RoutingStrategy.Bubble, typeof(DataObjectCopyingEventHandler), typeof(DataObject));

		// Token: 0x04001E74 RID: 7796
		public static readonly RoutedEvent PastingEvent = EventManager.RegisterRoutedEvent("Pasting", RoutingStrategy.Bubble, typeof(DataObjectPastingEventHandler), typeof(DataObject));

		// Token: 0x04001E75 RID: 7797
		public static readonly RoutedEvent SettingDataEvent = EventManager.RegisterRoutedEvent("SettingData", RoutingStrategy.Bubble, typeof(DataObjectSettingDataEventHandler), typeof(DataObject));

		// Token: 0x04001E76 RID: 7798
		private const string SystemDrawingBitmapFormat = "System.Drawing.Bitmap";

		// Token: 0x04001E77 RID: 7799
		private const string SystemBitmapSourceFormat = "System.Windows.Media.Imaging.BitmapSource";

		// Token: 0x04001E78 RID: 7800
		private const string SystemDrawingImagingMetafileFormat = "System.Drawing.Imaging.Metafile";

		// Token: 0x04001E79 RID: 7801
		private const int DV_E_FORMATETC = -2147221404;

		// Token: 0x04001E7A RID: 7802
		private const int DV_E_LINDEX = -2147221400;

		// Token: 0x04001E7B RID: 7803
		private const int DV_E_TYMED = -2147221399;

		// Token: 0x04001E7C RID: 7804
		private const int DV_E_DVASPECT = -2147221397;

		// Token: 0x04001E7D RID: 7805
		private const int OLE_E_NOTRUNNING = -2147221499;

		// Token: 0x04001E7E RID: 7806
		private const int OLE_E_ADVISENOTSUPPORTED = -2147221501;

		// Token: 0x04001E7F RID: 7807
		private const int DATA_S_SAMEFORMATETC = 262448;

		// Token: 0x04001E80 RID: 7808
		private const int STG_E_MEDIUMFULL = -2147286928;

		// Token: 0x04001E81 RID: 7809
		private const int FILEDROPBASESIZE = 20;

		// Token: 0x04001E82 RID: 7810
		private static readonly TYMED[] ALLOWED_TYMEDS;

		// Token: 0x04001E83 RID: 7811
		private IDataObject _innerData;

		// Token: 0x04001E84 RID: 7812
		private static readonly byte[] _serializedObjectID;

		// Token: 0x02000429 RID: 1065
		private class FormatEnumerator : IEnumFORMATETC
		{
			// Token: 0x06002DDF RID: 11743 RVA: 0x00125C84 File Offset: 0x00124284
			internal FormatEnumerator(DataObject dataObject)
			{
				string[] formats = dataObject.GetFormats();
				this._formats = new FORMATETC[(formats == null) ? 0 : formats.Length];
				if (formats != null)
				{
					for (int i = 0; i < formats.Length; i++)
					{
						string text = formats[i];
						FORMATETC formatetc = default(FORMATETC);
						formatetc.cfFormat = (short)DataFormats.GetDataFormat(text).Id;
						formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
						formatetc.ptd = IntPtr.Zero;
						formatetc.lindex = -1;
						if (DataObject.IsFormatEqual(text, DataFormats.Bitmap))
						{
							formatetc.tymed = TYMED.TYMED_GDI;
						}
						else if (DataObject.IsFormatEqual(text, DataFormats.EnhancedMetafile))
						{
							formatetc.tymed = TYMED.TYMED_ENHMF;
						}
						else
						{
							formatetc.tymed = TYMED.TYMED_HGLOBAL;
						}
						this._formats[i] = formatetc;
					}
				}
			}

			// Token: 0x06002DE0 RID: 11744 RVA: 0x00125D4C File Offset: 0x0012434C
			private FormatEnumerator(DataObject.FormatEnumerator formatEnumerator)
			{
				this._formats = formatEnumerator._formats;
				this._current = formatEnumerator._current;
			}

			// Token: 0x06002DE1 RID: 11745 RVA: 0x00125D6C File Offset: 0x0012436C
			public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
			{
				int num = 0;
				if (rgelt == null)
				{
					return -2147024809;
				}
				int num2 = 0;
				while (num2 < celt && this._current < this._formats.Length)
				{
					rgelt[num2] = this._formats[this._current];
					this._current++;
					num++;
					num2++;
				}
				if (pceltFetched != null)
				{
					pceltFetched[0] = num;
				}
				if (num != celt)
				{
					return 1;
				}
				return 0;
			}

			// Token: 0x06002DE2 RID: 11746 RVA: 0x00125DD8 File Offset: 0x001243D8
			public int Skip(int celt)
			{
				this._current += Math.Min(celt, int.MaxValue - this._current);
				if (this._current >= this._formats.Length)
				{
					return 1;
				}
				return 0;
			}

			// Token: 0x06002DE3 RID: 11747 RVA: 0x00125E0C File Offset: 0x0012440C
			public int Reset()
			{
				this._current = 0;
				return 0;
			}

			// Token: 0x06002DE4 RID: 11748 RVA: 0x00125E16 File Offset: 0x00124416
			public void Clone(out IEnumFORMATETC ppenum)
			{
				ppenum = new DataObject.FormatEnumerator(this);
			}

			// Token: 0x04001E8C RID: 7820
			private readonly FORMATETC[] _formats;

			// Token: 0x04001E8D RID: 7821
			private int _current;
		}

		// Token: 0x0200042A RID: 1066
		private class OleConverter : IDataObject
		{
			// Token: 0x06002DE5 RID: 11749 RVA: 0x00125E20 File Offset: 0x00124420
			public OleConverter(IDataObject data)
			{
				this._innerData = data;
			}

			// Token: 0x06002DE6 RID: 11750 RVA: 0x00125E2F File Offset: 0x0012442F
			public object GetData(string format)
			{
				return this.GetData(format, true);
			}

			// Token: 0x06002DE7 RID: 11751 RVA: 0x00125E39 File Offset: 0x00124439
			public object GetData(Type format)
			{
				return this.GetData(format.FullName);
			}

			// Token: 0x06002DE8 RID: 11752 RVA: 0x00125E47 File Offset: 0x00124447
			public object GetData(string format, bool autoConvert)
			{
				return this.GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
			}

			// Token: 0x06002DE9 RID: 11753 RVA: 0x00125E53 File Offset: 0x00124453
			public bool GetDataPresent(string format)
			{
				return this.GetDataPresent(format, true);
			}

			// Token: 0x06002DEA RID: 11754 RVA: 0x00125E5D File Offset: 0x0012445D
			public bool GetDataPresent(Type format)
			{
				return this.GetDataPresent(format.FullName);
			}

			// Token: 0x06002DEB RID: 11755 RVA: 0x00125E6B File Offset: 0x0012446B
			public bool GetDataPresent(string format, bool autoConvert)
			{
				return this.GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
			}

			// Token: 0x06002DEC RID: 11756 RVA: 0x00125E77 File Offset: 0x00124477
			public string[] GetFormats()
			{
				return this.GetFormats(true);
			}

			// Token: 0x06002DED RID: 11757 RVA: 0x00125E80 File Offset: 0x00124480
			public void SetData(object data)
			{
				if (data is ISerializable)
				{
					this.SetData(DataFormats.Serializable, data);
					return;
				}
				this.SetData(data.GetType(), data);
			}

			// Token: 0x06002DEE RID: 11758 RVA: 0x00125EA4 File Offset: 0x001244A4
			public string[] GetFormats(bool autoConvert)
			{
				ArrayList arrayList = new ArrayList();
				IEnumFORMATETC enumFORMATETC = this.EnumFormatEtcInner(DATADIR.DATADIR_GET);
				if (enumFORMATETC != null)
				{
					enumFORMATETC.Reset();
					FORMATETC[] array = new FORMATETC[1];
					int[] array2 = new int[]
					{
						1
					};
					while (array2[0] > 0)
					{
						array2[0] = 0;
						if (enumFORMATETC.Next(1, array, array2) == 0 && array2[0] > 0)
						{
							string name = DataFormats.GetDataFormat((int)array[0].cfFormat).Name;
							if (autoConvert)
							{
								string[] mappedFormats = DataObject.GetMappedFormats(name);
								for (int i = 0; i < mappedFormats.Length; i++)
								{
									arrayList.Add(mappedFormats[i]);
								}
							}
							else
							{
								arrayList.Add(name);
							}
							for (int j = 0; j < array.Length; j++)
							{
								if (array[j].ptd != IntPtr.Zero)
								{
									Marshal.FreeCoTaskMem(array[j].ptd);
								}
							}
						}
					}
				}
				string[] array3 = new string[arrayList.Count];
				arrayList.CopyTo(array3, 0);
				return DataObject.GetDistinctStrings(array3);
			}

			// Token: 0x06002DEF RID: 11759 RVA: 0x00125FB3 File Offset: 0x001245B3
			public void SetData(string format, object data)
			{
				this.SetData(format, data, true);
			}

			// Token: 0x06002DF0 RID: 11760 RVA: 0x00125FBE File Offset: 0x001245BE
			public void SetData(Type format, object data)
			{
				this.SetData(format.FullName, data);
			}

			// Token: 0x06002DF1 RID: 11761 RVA: 0x00125FCD File Offset: 0x001245CD
			public void SetData(string format, object data, bool autoConvert)
			{
				this.SetData(format, data, true, DVASPECT.DVASPECT_CONTENT, 0);
			}

			// Token: 0x170008B0 RID: 2224
			// (get) Token: 0x06002DF2 RID: 11762 RVA: 0x00125FDA File Offset: 0x001245DA
			public IDataObject OleDataObject
			{
				get
				{
					return this._innerData;
				}
			}

			// Token: 0x06002DF3 RID: 11763 RVA: 0x00125FE4 File Offset: 0x001245E4
			private object GetData(string format, bool autoConvert, DVASPECT aspect, int index)
			{
				object obj = this.GetDataFromBoundOleDataObject(format, aspect, index);
				object obj2 = obj;
				if (autoConvert && (obj == null || obj is MemoryStream))
				{
					string[] mappedFormats = DataObject.GetMappedFormats(format);
					if (mappedFormats != null)
					{
						for (int i = 0; i < mappedFormats.Length; i++)
						{
							if (!DataObject.IsFormatEqual(format, mappedFormats[i]))
							{
								obj = this.GetDataFromBoundOleDataObject(mappedFormats[i], aspect, index);
								if (obj != null && !(obj is MemoryStream))
								{
									if (DataObject.IsDataSystemBitmapSource(obj) || SystemDrawingHelper.IsBitmap(obj))
									{
										obj = DataObject.EnsureBitmapDataFromFormat(format, autoConvert, obj);
									}
									obj2 = null;
									break;
								}
							}
						}
					}
				}
				if (obj2 != null)
				{
					return obj2;
				}
				return obj;
			}

			// Token: 0x06002DF4 RID: 11764 RVA: 0x0012606C File Offset: 0x0012466C
			private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
			{
				bool dataPresentInner = this.GetDataPresentInner(format, aspect, index);
				if (!dataPresentInner && autoConvert)
				{
					string[] mappedFormats = DataObject.GetMappedFormats(format);
					if (mappedFormats != null)
					{
						for (int i = 0; i < mappedFormats.Length; i++)
						{
							if (!DataObject.IsFormatEqual(format, mappedFormats[i]))
							{
								dataPresentInner = this.GetDataPresentInner(mappedFormats[i], aspect, index);
								if (dataPresentInner)
								{
									break;
								}
							}
						}
					}
				}
				return dataPresentInner;
			}

			// Token: 0x06002DF5 RID: 11765 RVA: 0x001260C0 File Offset: 0x001246C0
			private void SetData(string format, object data, bool autoConvert, DVASPECT aspect, int index)
			{
				throw new InvalidOperationException(SR.Get("DataObject_CannotSetDataOnAFozenOLEDataDbject"));
			}

			// Token: 0x06002DF6 RID: 11766 RVA: 0x001260D4 File Offset: 0x001246D4
			private object GetDataFromOleIStream(string format, DVASPECT aspect, int index)
			{
				FORMATETC formatetc = default(FORMATETC);
				formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
				formatetc.dwAspect = aspect;
				formatetc.lindex = index;
				formatetc.tymed = TYMED.TYMED_ISTREAM;
				object result = null;
				if (this.QueryGetDataInner(ref formatetc) == 0)
				{
					STGMEDIUM stgmedium;
					this.GetDataInner(ref formatetc, out stgmedium);
					try
					{
						if (stgmedium.unionmember != IntPtr.Zero && stgmedium.tymed == TYMED.TYMED_ISTREAM)
						{
							UnsafeNativeMethods.IStream stream = (UnsafeNativeMethods.IStream)Marshal.GetObjectForIUnknown(stgmedium.unionmember);
							NativeMethods.STATSTG statstg = new NativeMethods.STATSTG();
							stream.Stat(statstg, 0);
							int num = (int)statstg.cbSize;
							IntPtr intPtr = DataObject.Win32GlobalAlloc(8258, (IntPtr)num);
							try
							{
								IntPtr buf = DataObject.Win32GlobalLock(new HandleRef(this, intPtr));
								try
								{
									stream.Seek(0L, 0);
									stream.Read(buf, num);
								}
								finally
								{
									DataObject.Win32GlobalUnlock(new HandleRef(this, intPtr));
								}
								result = this.GetDataFromHGLOBAL(format, intPtr);
							}
							finally
							{
								DataObject.Win32GlobalFree(new HandleRef(this, intPtr));
							}
						}
					}
					finally
					{
						UnsafeNativeMethods.ReleaseStgMedium(ref stgmedium);
					}
				}
				return result;
			}

			// Token: 0x06002DF7 RID: 11767 RVA: 0x00126210 File Offset: 0x00124810
			private object GetDataFromHGLOBAL(string format, IntPtr hglobal)
			{
				object result = null;
				if (hglobal != IntPtr.Zero)
				{
					if (DataObject.IsFormatEqual(format, DataFormats.Html) || DataObject.IsFormatEqual(format, DataFormats.Xaml))
					{
						result = this.ReadStringFromHandleUtf8(hglobal);
					}
					else if (DataObject.IsFormatEqual(format, DataFormats.Text) || DataObject.IsFormatEqual(format, DataFormats.Rtf) || DataObject.IsFormatEqual(format, DataFormats.OemText) || DataObject.IsFormatEqual(format, DataFormats.CommaSeparatedValue))
					{
						result = this.ReadStringFromHandle(hglobal, false);
					}
					else if (DataObject.IsFormatEqual(format, DataFormats.UnicodeText) || DataObject.IsFormatEqual(format, DataFormats.ApplicationTrust))
					{
						result = this.ReadStringFromHandle(hglobal, true);
					}
					else if (DataObject.IsFormatEqual(format, DataFormats.FileDrop))
					{
						result = this.ReadFileListFromHandle(hglobal);
					}
					else if (DataObject.IsFormatEqual(format, DataFormats.FileName))
					{
						result = new string[]
						{
							this.ReadStringFromHandle(hglobal, false)
						};
					}
					else if (DataObject.IsFormatEqual(format, DataFormats.FileNameW))
					{
						result = new string[]
						{
							this.ReadStringFromHandle(hglobal, true)
						};
					}
					else if (DataObject.IsFormatEqual(format, typeof(BitmapSource).FullName))
					{
						result = this.ReadBitmapSourceFromHandle(hglobal);
					}
					else
					{
						bool restrictDeserialization = DataObject.IsFormatEqual(format, DataFormats.StringFormat) || DataObject.IsFormatEqual(format, DataFormats.Dib) || DataObject.IsFormatEqual(format, DataFormats.Bitmap) || DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile) || DataObject.IsFormatEqual(format, DataFormats.MetafilePicture) || DataObject.IsFormatEqual(format, DataFormats.SymbolicLink) || DataObject.IsFormatEqual(format, DataFormats.Dif) || DataObject.IsFormatEqual(format, DataFormats.Tiff) || DataObject.IsFormatEqual(format, DataFormats.Palette) || DataObject.IsFormatEqual(format, DataFormats.PenData) || DataObject.IsFormatEqual(format, DataFormats.Riff) || DataObject.IsFormatEqual(format, DataFormats.WaveAudio) || DataObject.IsFormatEqual(format, DataFormats.Locale);
						result = this.ReadObjectFromHandle(hglobal, restrictDeserialization);
					}
				}
				return result;
			}

			// Token: 0x06002DF8 RID: 11768 RVA: 0x00126404 File Offset: 0x00124A04
			private object GetDataFromOleHGLOBAL(string format, DVASPECT aspect, int index)
			{
				FORMATETC formatetc = default(FORMATETC);
				formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
				formatetc.dwAspect = aspect;
				formatetc.lindex = index;
				formatetc.tymed = TYMED.TYMED_HGLOBAL;
				object result = null;
				if (this.QueryGetDataInner(ref formatetc) == 0)
				{
					STGMEDIUM stgmedium;
					this.GetDataInner(ref formatetc, out stgmedium);
					try
					{
						if (stgmedium.unionmember != IntPtr.Zero && stgmedium.tymed == TYMED.TYMED_HGLOBAL)
						{
							result = this.GetDataFromHGLOBAL(format, stgmedium.unionmember);
						}
					}
					finally
					{
						UnsafeNativeMethods.ReleaseStgMedium(ref stgmedium);
					}
				}
				return result;
			}

			// Token: 0x06002DF9 RID: 11769 RVA: 0x001264A0 File Offset: 0x00124AA0
			private object GetDataFromOleOther(string format, DVASPECT aspect, int index)
			{
				FORMATETC formatetc = default(FORMATETC);
				TYMED tymed = TYMED.TYMED_NULL;
				if (DataObject.IsFormatEqual(format, DataFormats.Bitmap))
				{
					tymed = TYMED.TYMED_GDI;
				}
				else if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile))
				{
					tymed = TYMED.TYMED_ENHMF;
				}
				if (tymed == TYMED.TYMED_NULL)
				{
					return null;
				}
				formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
				formatetc.dwAspect = aspect;
				formatetc.lindex = index;
				formatetc.tymed = tymed;
				object result = null;
				if (this.QueryGetDataInner(ref formatetc) == 0)
				{
					STGMEDIUM stgmedium;
					this.GetDataInner(ref formatetc, out stgmedium);
					try
					{
						if (stgmedium.unionmember != IntPtr.Zero)
						{
							if (DataObject.IsFormatEqual(format, DataFormats.Bitmap))
							{
								result = this.GetBitmapSourceFromHbitmap(stgmedium.unionmember);
							}
							else if (DataObject.IsFormatEqual(format, DataFormats.EnhancedMetafile))
							{
								result = SystemDrawingHelper.GetMetafileFromHemf(stgmedium.unionmember);
							}
						}
					}
					finally
					{
						UnsafeNativeMethods.ReleaseStgMedium(ref stgmedium);
					}
				}
				return result;
			}

			// Token: 0x06002DFA RID: 11770 RVA: 0x00126584 File Offset: 0x00124B84
			private object GetDataFromBoundOleDataObject(string format, DVASPECT aspect, int index)
			{
				object obj = this.GetDataFromOleOther(format, aspect, index);
				if (obj == null)
				{
					obj = this.GetDataFromOleHGLOBAL(format, aspect, index);
				}
				if (obj == null)
				{
					obj = this.GetDataFromOleIStream(format, aspect, index);
				}
				return obj;
			}

			// Token: 0x06002DFB RID: 11771 RVA: 0x001265B8 File Offset: 0x00124BB8
			private Stream ReadByteStreamFromHandle(IntPtr handle, out bool isSerializedObject)
			{
				IntPtr source = DataObject.Win32GlobalLock(new HandleRef(this, handle));
				Stream result;
				try
				{
					int num = NativeMethods.IntPtrToInt32(DataObject.Win32GlobalSize(new HandleRef(this, handle)));
					byte[] array = new byte[num];
					Marshal.Copy(source, array, 0, num);
					int num2 = 0;
					if (num > DataObject._serializedObjectID.Length)
					{
						isSerializedObject = true;
						for (int i = 0; i < DataObject._serializedObjectID.Length; i++)
						{
							if (DataObject._serializedObjectID[i] != array[i])
							{
								isSerializedObject = false;
								break;
							}
						}
						if (isSerializedObject)
						{
							num2 = DataObject._serializedObjectID.Length;
						}
					}
					else
					{
						isSerializedObject = false;
					}
					result = new MemoryStream(array, num2, array.Length - num2);
				}
				finally
				{
					DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
				}
				return result;
			}

			// Token: 0x06002DFC RID: 11772 RVA: 0x0012666C File Offset: 0x00124C6C
			private object ReadObjectFromHandle(IntPtr handle, bool restrictDeserialization)
			{
				object result = null;
				bool flag;
				Stream stream = this.ReadByteStreamFromHandle(handle, out flag);
				if (flag)
				{
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					if (restrictDeserialization)
					{
						binaryFormatter.Binder = new DataObject.OleConverter.TypeRestrictingSerializationBinder();
					}
					try
					{
						return binaryFormatter.Deserialize(stream);
					}
					catch (DataObject.OleConverter.RestrictedTypeDeserializationException)
					{
						return null;
					}
				}
				result = stream;
				return result;
			}

			// Token: 0x06002DFD RID: 11773 RVA: 0x001266C0 File Offset: 0x00124CC0
			private BitmapSource ReadBitmapSourceFromHandle(IntPtr handle)
			{
				BitmapSource result = null;
				bool flag;
				Stream stream = this.ReadByteStreamFromHandle(handle, out flag);
				if (stream != null)
				{
					result = BitmapFrame.Create(stream);
				}
				return result;
			}

			// Token: 0x06002DFE RID: 11774 RVA: 0x001266E4 File Offset: 0x00124CE4
			private string[] ReadFileListFromHandle(IntPtr hdrop)
			{
				string[] array = null;
				StringBuilder stringBuilder = new StringBuilder(260);
				int num = UnsafeNativeMethods.DragQueryFile(new HandleRef(this, hdrop), -1, null, 0);
				if (num > 0)
				{
					array = new string[num];
					for (int i = 0; i < num; i++)
					{
						if (UnsafeNativeMethods.DragQueryFile(new HandleRef(this, hdrop), i, stringBuilder, stringBuilder.Capacity) != 0)
						{
							array[i] = stringBuilder.ToString();
						}
					}
				}
				return array;
			}

			// Token: 0x06002DFF RID: 11775 RVA: 0x00126748 File Offset: 0x00124D48
			private unsafe string ReadStringFromHandle(IntPtr handle, bool unicode)
			{
				string result = null;
				IntPtr value = DataObject.Win32GlobalLock(new HandleRef(this, handle));
				try
				{
					if (unicode)
					{
						result = new string((char*)((void*)value));
					}
					else
					{
						result = new string((sbyte*)((void*)value));
					}
				}
				finally
				{
					DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
				}
				return result;
			}

			// Token: 0x06002E00 RID: 11776 RVA: 0x001267A4 File Offset: 0x00124DA4
			private string ReadStringFromHandleUtf8(IntPtr handle)
			{
				string result = null;
				int num = NativeMethods.IntPtrToInt32(DataObject.Win32GlobalSize(new HandleRef(this, handle)));
				IntPtr intPtr = DataObject.Win32GlobalLock(new HandleRef(this, handle));
				try
				{
					int num2 = 0;
					while (num2 < num && Marshal.ReadByte((IntPtr)((long)intPtr + (long)num2)) != 0)
					{
						num2++;
					}
					if (num2 > 0)
					{
						byte[] array = new byte[num2];
						Marshal.Copy(intPtr, array, 0, num2);
						result = new UTF8Encoding().GetString(array, 0, num2);
					}
				}
				finally
				{
					DataObject.Win32GlobalUnlock(new HandleRef(this, handle));
				}
				return result;
			}

			// Token: 0x06002E01 RID: 11777 RVA: 0x00126838 File Offset: 0x00124E38
			private bool GetDataPresentInner(string format, DVASPECT aspect, int index)
			{
				FORMATETC formatetc = default(FORMATETC);
				formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
				formatetc.dwAspect = aspect;
				formatetc.lindex = index;
				for (int i = 0; i < DataObject.ALLOWED_TYMEDS.Length; i++)
				{
					formatetc.tymed |= DataObject.ALLOWED_TYMEDS[i];
				}
				return this.QueryGetDataInner(ref formatetc) == 0;
			}

			// Token: 0x06002E02 RID: 11778 RVA: 0x0012689F File Offset: 0x00124E9F
			private int QueryGetDataInner(ref FORMATETC formatetc)
			{
				return this._innerData.QueryGetData(ref formatetc);
			}

			// Token: 0x06002E03 RID: 11779 RVA: 0x001268AD File Offset: 0x00124EAD
			private void GetDataInner(ref FORMATETC formatetc, out STGMEDIUM medium)
			{
				this._innerData.GetData(ref formatetc, out medium);
			}

			// Token: 0x06002E04 RID: 11780 RVA: 0x001268BC File Offset: 0x00124EBC
			private IEnumFORMATETC EnumFormatEtcInner(DATADIR dwDirection)
			{
				return this._innerData.EnumFormatEtc(dwDirection);
			}

			// Token: 0x06002E05 RID: 11781 RVA: 0x001268CA File Offset: 0x00124ECA
			[MethodImpl(MethodImplOptions.NoInlining)]
			private object GetBitmapSourceFromHbitmap(IntPtr hbitmap)
			{
				return Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, null);
			}

			// Token: 0x04001E8E RID: 7822
			internal IDataObject _innerData;

			// Token: 0x0200042B RID: 1067
			private class TypeRestrictingSerializationBinder : SerializationBinder
			{
				// Token: 0x06002E07 RID: 11783 RVA: 0x001268E5 File Offset: 0x00124EE5
				public override Type BindToType(string assemblyName, string typeName)
				{
					throw new DataObject.OleConverter.RestrictedTypeDeserializationException();
				}
			}

			// Token: 0x0200042C RID: 1068
			private class RestrictedTypeDeserializationException : Exception
			{
			}
		}

		// Token: 0x02000430 RID: 1072
		private class DataStore : IDataObject
		{
			// Token: 0x06002E2F RID: 11823 RVA: 0x00126DA0 File Offset: 0x001253A0
			public object GetData(string format)
			{
				return this.GetData(format, true);
			}

			// Token: 0x06002E30 RID: 11824 RVA: 0x00126DAA File Offset: 0x001253AA
			public object GetData(Type format)
			{
				return this.GetData(format.FullName);
			}

			// Token: 0x06002E31 RID: 11825 RVA: 0x00126DB8 File Offset: 0x001253B8
			public object GetData(string format, bool autoConvert)
			{
				return this.GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
			}

			// Token: 0x06002E32 RID: 11826 RVA: 0x00126DC4 File Offset: 0x001253C4
			public bool GetDataPresent(string format)
			{
				return this.GetDataPresent(format, true);
			}

			// Token: 0x06002E33 RID: 11827 RVA: 0x00126DCE File Offset: 0x001253CE
			public bool GetDataPresent(Type format)
			{
				return this.GetDataPresent(format.FullName);
			}

			// Token: 0x06002E34 RID: 11828 RVA: 0x00126DDC File Offset: 0x001253DC
			public bool GetDataPresent(string format, bool autoConvert)
			{
				return this.GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
			}

			// Token: 0x06002E35 RID: 11829 RVA: 0x00126DE8 File Offset: 0x001253E8
			public string[] GetFormats()
			{
				return this.GetFormats(true);
			}

			// Token: 0x06002E36 RID: 11830 RVA: 0x00126DF4 File Offset: 0x001253F4
			public string[] GetFormats(bool autoConvert)
			{
				bool flag = false;
				string[] array = new string[this._data.Keys.Count];
				this._data.Keys.CopyTo(array, 0);
				if (autoConvert)
				{
					ArrayList arrayList = new ArrayList();
					for (int i = 0; i < array.Length; i++)
					{
						DataObject.DataStore.DataStoreEntry[] array2 = (DataObject.DataStore.DataStoreEntry[])this._data[array[i]];
						bool flag2 = true;
						for (int j = 0; j < array2.Length; j++)
						{
							if (!array2[j].AutoConvert)
							{
								flag2 = false;
								break;
							}
						}
						if (flag2)
						{
							string[] mappedFormats = DataObject.GetMappedFormats(array[i]);
							for (int k = 0; k < mappedFormats.Length; k++)
							{
								bool flag3 = false;
								int num = 0;
								while (!flag3 && num < array2.Length)
								{
									if (DataObject.IsFormatAndDataSerializable(mappedFormats[k], array2[num].Data) && flag)
									{
										flag = true;
										flag3 = true;
									}
									num++;
								}
								if (!flag3)
								{
									arrayList.Add(mappedFormats[k]);
								}
							}
						}
						else if (!flag)
						{
							arrayList.Add(array[i]);
						}
					}
					string[] array3 = new string[arrayList.Count];
					arrayList.CopyTo(array3, 0);
					array = DataObject.GetDistinctStrings(array3);
				}
				return array;
			}

			// Token: 0x06002E37 RID: 11831 RVA: 0x00126F22 File Offset: 0x00125522
			public void SetData(object data)
			{
				if (data is ISerializable && !this._data.ContainsKey(DataFormats.Serializable))
				{
					this.SetData(DataFormats.Serializable, data);
				}
				this.SetData(data.GetType(), data);
			}

			// Token: 0x06002E38 RID: 11832 RVA: 0x00126F57 File Offset: 0x00125557
			public void SetData(string format, object data)
			{
				this.SetData(format, data, true);
			}

			// Token: 0x06002E39 RID: 11833 RVA: 0x00126F62 File Offset: 0x00125562
			public void SetData(Type format, object data)
			{
				this.SetData(format.FullName, data);
			}

			// Token: 0x06002E3A RID: 11834 RVA: 0x00126F71 File Offset: 0x00125571
			public void SetData(string format, object data, bool autoConvert)
			{
				if (DataObject.IsFormatEqual(format, DataFormats.Dib) && autoConvert && (SystemDrawingHelper.IsBitmap(data) || DataObject.IsDataSystemBitmapSource(data)))
				{
					format = DataFormats.Bitmap;
				}
				this.SetData(format, data, autoConvert, DVASPECT.DVASPECT_CONTENT, 0);
			}

			// Token: 0x06002E3B RID: 11835 RVA: 0x00126FA8 File Offset: 0x001255A8
			private object GetData(string format, bool autoConvert, DVASPECT aspect, int index)
			{
				DataObject.DataStore.DataStoreEntry dataStoreEntry = this.FindDataStoreEntry(format, aspect, index);
				object obj = this.GetDataFromDataStoreEntry(dataStoreEntry, format);
				object obj2 = obj;
				if (autoConvert && (dataStoreEntry == null || dataStoreEntry.AutoConvert) && (obj == null || obj is MemoryStream))
				{
					string[] mappedFormats = DataObject.GetMappedFormats(format);
					if (mappedFormats != null)
					{
						for (int i = 0; i < mappedFormats.Length; i++)
						{
							if (!DataObject.IsFormatEqual(format, mappedFormats[i]))
							{
								DataObject.DataStore.DataStoreEntry dataStoreEntry2 = this.FindDataStoreEntry(mappedFormats[i], aspect, index);
								obj = this.GetDataFromDataStoreEntry(dataStoreEntry2, mappedFormats[i]);
								if (obj != null && !(obj is MemoryStream))
								{
									if (DataObject.IsDataSystemBitmapSource(obj) || SystemDrawingHelper.IsBitmap(obj))
									{
										obj = DataObject.EnsureBitmapDataFromFormat(format, autoConvert, obj);
									}
									obj2 = null;
									break;
								}
							}
						}
					}
				}
				if (obj2 != null)
				{
					return obj2;
				}
				return obj;
			}

			// Token: 0x06002E3C RID: 11836 RVA: 0x0012705C File Offset: 0x0012565C
			private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
			{
				if (autoConvert)
				{
					string[] formats = this.GetFormats(autoConvert);
					for (int i = 0; i < formats.Length; i++)
					{
						if (DataObject.IsFormatEqual(format, formats[i]))
						{
							return true;
						}
					}
					return false;
				}
				if (!this._data.ContainsKey(format))
				{
					return false;
				}
				DataObject.DataStore.DataStoreEntry[] array = (DataObject.DataStore.DataStoreEntry[])this._data[format];
				DataObject.DataStore.DataStoreEntry dataStoreEntry = null;
				DataObject.DataStore.DataStoreEntry dataStoreEntry2 = null;
				foreach (DataObject.DataStore.DataStoreEntry dataStoreEntry3 in array)
				{
					if (dataStoreEntry3.Aspect == aspect && (index == -1 || dataStoreEntry3.Index == index))
					{
						dataStoreEntry = dataStoreEntry3;
						break;
					}
					if (dataStoreEntry3.Aspect == DVASPECT.DVASPECT_CONTENT && dataStoreEntry3.Index == 0)
					{
						dataStoreEntry2 = dataStoreEntry3;
					}
				}
				if (dataStoreEntry == null && dataStoreEntry2 != null)
				{
					dataStoreEntry = dataStoreEntry2;
				}
				return dataStoreEntry != null;
			}

			// Token: 0x06002E3D RID: 11837 RVA: 0x00127114 File Offset: 0x00125714
			private void SetData(string format, object data, bool autoConvert, DVASPECT aspect, int index)
			{
				DataObject.DataStore.DataStoreEntry dataStoreEntry = new DataObject.DataStore.DataStoreEntry(data, autoConvert, aspect, index);
				DataObject.DataStore.DataStoreEntry[] array = (DataObject.DataStore.DataStoreEntry[])this._data[format];
				if (array == null)
				{
					array = (DataObject.DataStore.DataStoreEntry[])Array.CreateInstance(typeof(DataObject.DataStore.DataStoreEntry), 1);
				}
				else
				{
					DataObject.DataStore.DataStoreEntry[] array2 = (DataObject.DataStore.DataStoreEntry[])Array.CreateInstance(typeof(DataObject.DataStore.DataStoreEntry), array.Length + 1);
					array.CopyTo(array2, 1);
					array = array2;
				}
				array[0] = dataStoreEntry;
				this._data[format] = array;
			}

			// Token: 0x06002E3E RID: 11838 RVA: 0x00127190 File Offset: 0x00125790
			private DataObject.DataStore.DataStoreEntry FindDataStoreEntry(string format, DVASPECT aspect, int index)
			{
				DataObject.DataStore.DataStoreEntry[] array = (DataObject.DataStore.DataStoreEntry[])this._data[format];
				DataObject.DataStore.DataStoreEntry dataStoreEntry = null;
				DataObject.DataStore.DataStoreEntry dataStoreEntry2 = null;
				if (array != null)
				{
					foreach (DataObject.DataStore.DataStoreEntry dataStoreEntry3 in array)
					{
						if (dataStoreEntry3.Aspect == aspect && (index == -1 || dataStoreEntry3.Index == index))
						{
							dataStoreEntry = dataStoreEntry3;
							break;
						}
						if (dataStoreEntry3.Aspect == DVASPECT.DVASPECT_CONTENT && dataStoreEntry3.Index == 0)
						{
							dataStoreEntry2 = dataStoreEntry3;
						}
					}
				}
				if (dataStoreEntry == null && dataStoreEntry2 != null)
				{
					dataStoreEntry = dataStoreEntry2;
				}
				return dataStoreEntry;
			}

			// Token: 0x06002E3F RID: 11839 RVA: 0x00127208 File Offset: 0x00125808
			private object GetDataFromDataStoreEntry(DataObject.DataStore.DataStoreEntry dataStoreEntry, string format)
			{
				object result = null;
				if (dataStoreEntry != null)
				{
					result = dataStoreEntry.Data;
				}
				return result;
			}

			// Token: 0x04001E96 RID: 7830
			private Hashtable _data = new Hashtable();

			// Token: 0x02000431 RID: 1073
			private class DataStoreEntry
			{
				// Token: 0x06002E40 RID: 11840 RVA: 0x00127222 File Offset: 0x00125822
				public DataStoreEntry(object data, bool autoConvert, DVASPECT aspect, int index)
				{
					this._data = data;
					this._autoConvert = autoConvert;
					this._aspect = aspect;
					this._index = index;
				}

				// Token: 0x170008BC RID: 2236
				// (get) Token: 0x06002E41 RID: 11841 RVA: 0x00127247 File Offset: 0x00125847
				// (set) Token: 0x06002E42 RID: 11842 RVA: 0x0012724F File Offset: 0x0012584F
				public object Data
				{
					get
					{
						return this._data;
					}
					set
					{
						this._data = value;
					}
				}

				// Token: 0x170008BD RID: 2237
				// (get) Token: 0x06002E43 RID: 11843 RVA: 0x00127258 File Offset: 0x00125858
				public bool AutoConvert
				{
					get
					{
						return this._autoConvert;
					}
				}

				// Token: 0x170008BE RID: 2238
				// (get) Token: 0x06002E44 RID: 11844 RVA: 0x00127260 File Offset: 0x00125860
				public DVASPECT Aspect
				{
					get
					{
						return this._aspect;
					}
				}

				// Token: 0x170008BF RID: 2239
				// (get) Token: 0x06002E45 RID: 11845 RVA: 0x00127268 File Offset: 0x00125868
				public int Index
				{
					get
					{
						return this._index;
					}
				}

				// Token: 0x04001E97 RID: 7831
				private object _data;

				// Token: 0x04001E98 RID: 7832
				private bool _autoConvert;

				// Token: 0x04001E99 RID: 7833
				private DVASPECT _aspect;

				// Token: 0x04001E9A RID: 7834
				private int _index;
			}
		}
	}
}
