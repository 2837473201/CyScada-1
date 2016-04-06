﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using CyScada.DAL;
using CyScada.Model;
using CyScada.Common;

namespace CyScada.BLL
{
    public class BllEmployee
    {

        private DalEmployee _dalEquipment = new DalEmployee();

        public DalEmployee DalEquipment
        {
            get { return _dalEquipment; }
            set { _dalEquipment = value; }
        }

        public IEnumerable<EmployeeModel> GetEmployeeList()
        {
            return GetEmployeeList(new EmployeeModel());
        }



        public IEnumerable<EmployeeModel> GetEmployeeList(EmployeeModel filterModel)
        {
            var dtEmployees = _dalEquipment.GetEmployeeList(filterModel.ToHashTable());
            return dtEmployees.AsEnumerable().Select(dr => new EmployeeModel
            {
                Authority = Convert.ToInt32(dr["Authority"]),
                Code = dr["Code"].ToString(),
                Description = dr["Description"].ToString(),
                Id = Convert.ToInt32(dr["Id"]),
                LoginName = dr["LoginName"].ToString(),
                Name = dr["Name"].ToString(),
                Password = dr["Password"].ToString()
            });


        }


    }
}
