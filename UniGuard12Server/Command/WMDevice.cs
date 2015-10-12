using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase.Command;
using Jwm.Device.Lib2;
using UniGuardLib;

namespace UniGuard12Server.Command
{
    public class WMDevice : CommandBase<GPRSSession, BinaryCommandInfo>
    {
        public override string Name
        {
            get { return "WMDEVICE"; }
        }

        public override void ExecuteCommand(GPRSSession session, BinaryCommandInfo commandInfo)
        {
            byte[] data = commandInfo.Data;
            WmDevice wmDevice = new WmDevice();
            List<object> dataList = wmDevice.parseData(data, data.Length);

            Log.Test("Gprs Testing", "Stepping through GPRS testing...");

            // Check that the Datatype is WM5000LT
            if (this.IsUnitReading(wmDevice))
            {
                // Add necessary data
                GPRSData gprsData = new GPRSData();
                gprsData.Agency         = wmDevice.Agency;
                gprsData.RecorderSerial = wmDevice.ReaderNumber;
                gprsData.Imei           = wmDevice.Imei;

                // Check that recorder authenticates
                if (gprsData.Authenticated)
                {
                    if (dataList != null && dataList.Count > 0)
                    {
                        // Records 
                        for (int i = 0; i < dataList.Count; i++)
                        {
                            this.ProcessGPRSRecord(wmDevice, dataList[i], gprsData);
                        }

                        // send received data ok command
                        session.SendResponse(wmDevice.ResponseBytes);
                    }
                }
                else
                {
                    // Log unauthenticated attempts
                    Log.Warning(
                        "Unauthenticated GPRS recorder attempted to connect to server.\r\n" +
                        "=> Agent : " + wmDevice.Agency + "\r\n" +
                        "=> Serial: " + wmDevice.ReaderNumber
                    );
                }
            }
            
            if (wmDevice.Devicetype == DeviceType.TIMING)
            {
                session.SendResponse(wmDevice.ResponseBytes);
            }
        }

        private void ProcessGPRSRecord(WmDevice wmDevice, object data, GPRSData gprsData)
        {
            Record record = this.CastRecord(wmDevice, data);
            string recordType = record.Recordtype.ToString();
            gprsData.Database = Hacks.AdjustDatabase(gprsData.Database);

            // GPS enabled recorders
            if (wmDevice.Devicetype == DeviceType.WM5000P5)
            {
                this.StoreGPSData(data, recordType);
            }
            else
            {
                this.StoreNonGPSData(gprsData, record);
            }
        }

        private void StoreGPSData(object data, string recordType)
        {
            switch (recordType)
            {
                case "Normal":
                case "ManuAlarm":
                case "LowVoltage":
                case "CustomButton":
                case "GPSCheckPoint":
                case "GPSPosition":
                case "GPSTrail":
                    WM5000P5Record gpsRecord = (WM5000P5Record) data;

                    StringBuilder sb = new StringBuilder();
                    sb.Append("AddressID: " + gpsRecord.AddressID + Environment.NewLine);
                    sb.Append("Read time: " + gpsRecord.ReadTime.ToString() + Environment.NewLine);
                    sb.Append("Information: " + gpsRecord.Information + Environment.NewLine);
                    sb.Append("Longitude: " + gpsRecord.Longitude + Environment.NewLine);
                    sb.Append("Latitude: " + gpsRecord.Latitude + Environment.NewLine);
                    sb.Append("Speed: " + gpsRecord.Speed + Environment.NewLine);
                    sb.Append("Satellite: " + gpsRecord.Satellites + Environment.NewLine);

                    // Not currently storing to data store...
                    Log.Test(recordType, sb.ToString());
                    break;
            }
        }

        private void StoreNonGPSData(GPRSData gprsData, Record record)
        {
            string recordType = record.Recordtype.ToString();
            switch (recordType)
            {
                case "Normal":
                    gprsData.AddNormalRecord(new string[] {
                        Utility.HexToDec(record.AddressID),
                        record.ReadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        gprsData.RecorderSerial
                    });
                    break;

                case "ManuAlarm":
                    gprsData.AddAlarmRecord(new string[] {
                        record.ReadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        gprsData.RecorderSerial
                    });
                    break;

                case "LowVoltage":
                    gprsData.AddLowVoltageRecord(new string[] {
                        record.ReadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        gprsData.RecorderSerial,
                        record.Information
                    });
                    break;

                case "CustomButton":
                    gprsData.AddCustomRecord(new string[] {
                        record.ReadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        gprsData.RecorderSerial
                    });
                    break;
            }

            // Store the data
            gprsData.StoreData();
        }

        private bool IsUnitReading(WmDevice wmDevice)
        {
            return (
                wmDevice.Devicetype == DeviceType.WM5000LT ||
                wmDevice.Devicetype == DeviceType.WM5000L5 ||
                wmDevice.Devicetype == DeviceType.WM5000P5 ||
                wmDevice.Devicetype == DeviceType.WM5000LG
            );
        }

        private Record CastRecord(WmDevice wmDevice, object data)
        {
            Record record = null;
            switch (wmDevice.Devicetype)
            {
                case DeviceType.WM5000LT:
                    record = (WM5000LTRecord) data;
                    break;

                case DeviceType.WM5000L5: 
                    record = (WM5000L5Record) data;
                    break;

                case DeviceType.WM5000P5: 
                    record = (WM5000P5Record) data;
                    break;

                case DeviceType.WM5000LG: 
                    record = (WM5000LGRecord) data;
                    break;
            }

            return record;
        }

    }
}
