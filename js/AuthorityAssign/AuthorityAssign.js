﻿"use strict";

angular.module("AuthorityAssign", [])
    .controller("AuthorityAssignController", ['$scope', '$http', function($scope, $http) {
        $scope.initial = function() {
            $scope.userNameConfig = {
                placeholder: '输入用户名称'
            };


            //获取列表
            $http.get("../api/AuthorityAssign")
                .success(function(data) {
                    $('#userTable').bootstrapTable('load', data[0]);
                    $('#roleTable').bootstrapTable('load', data[1]);
                }).error(function(error) {
                    alert(error);
                });
            
        };


        $scope.Query = function() {
            //var params = $scope.employee;
            //$http.get("../api/EmployeeList/?paramstring=" + encodeURI(JSON.stringify(params)))
            //    .success(function (data) {
            //        $('#ListTable').bootstrapTable('load', data);
            //    }).error(function (error) {
            //        if (error.status == 403) {
            //            window.location.href = "/Account/Login?ReturnUrl=" + window.location.pathname;
            //        } else {
            //            modal.alertMsg("加载失败！");
            //        }
            //    });
        };


        $scope.toggleRole = function(obj) {
            var toggleType;
            if (obj.role.displayClass.indexOf('UserRole') > -1) {
                toggleType = "Delete";
            } else {
                toggleType = "Add";
            }

            $.ajax({
                url: '../api/AuthorityAssign',
                type: 'POST',
                data: { 'UserId': $scope.info.Id, "ToggleType": toggleType, 'Id': obj.role.Id, 'ToggleHost': 'UserRole' },
                success: function(result) {
                    if (result == '') {
                        $scope.refreshUserAuthority($scope.info.Id);
                    } else {
                        alert(result);
                    }
                },
                error: function(error) {
                    alert(error);
                }
            });
        };

        $scope.toggleAuthority = function(obj) {
            var toggleType;
            if (obj.authority.displayClass.indexOf('UserAuth') > -1) {
                toggleType = "Delete";
            } else {
                toggleType = "Add";
            }

            $.ajax({
                url: '../api/AuthorityAssign',
                type: 'POST',
                data: { 'UserId': $scope.info.Id, "ToggleType": toggleType, 'Id': obj.authority.AuthorityId, 'ToggleHost': 'UserAuthority' },
                success: function(result) {
                    if (result == '') {
                        $scope.refreshUserAuthority($scope.info.Id);
                    } else {
                        alert(result);
                    }
                },
                error: function(error) {
                    alert(error);
                }
            });
        };

        $scope.toggleRoleAuthority = function (obj) {
            var toggleType;
            if (obj.authority.displayClass.indexOf('RoleAuth') > -1) {
                toggleType = "Delete";
            } else {
                toggleType = "Add";
            }

            $.ajax({
                url: '../api/AuthorityAssign',
                type: 'POST',
                data: { 'UserId': $scope.roleInfo.Id, "ToggleType": toggleType, 'Id': obj.authority.AuthorityId, 'ToggleHost': 'RoleAuthority' },
                success: function (result) {
                    if (result == '') {
                        $scope.refreshRoleAuthority($scope.roleInfo.Id);
                    } else {
                        alert(result);
                    }
                },
                error: function (error) {
                    alert(error);
                }
            });
        };




        $scope.initial();

        $scope.refreshUserAuthority = function(userId) {
            $.ajax({
                url: '../api/AuthorityAssign?userId=' + userId,
                type: 'GET',
                success: function(data) {
                    if (data != '') {
                        $scope.info = data;
                        $scope.info.title = data.Name + "的权限";
                        $scope.info.CurrentAuthorityList = [];
                        var authList = [];
                        authList = jQuery.extend(true, [], data.AuthorityList);
                        
                        //根据角色合并角色的所有权限
                        var auth = 0;//用户的当前权限汇总
                        var roleAuth = 0;//角色的权限汇总
                        for (var i = 0; i < data.EmpRoleList.length; i++) {
                            for (var j = 0; j < data.RoleList.length; j++) {
                                if (data.EmpRoleList[i].RoleId == data.RoleList[j].Id) {
                                    roleAuth = roleAuth | data.RoleList[j].Authority;
                                }
                            }
                        }
                        //合并用户的附加权限
                        auth = roleAuth | data.Authority;
                        //解析权限(将当前用户的权限填写到当前权限列表)
                        for (var k = 0; k < authList.length; k++) {
                            if ((authList[k].AuthorityId & auth) == authList[k].AuthorityId) {
                                authList[k].displayClass = 'label AuthLabel  NoAuth';
                                $scope.info.CurrentAuthorityList.push(authList[k]);
                            }
                        }

                        $scope.$apply();
                        //修改各权限列表的样式(添加颜色)
                        //当前权限列表
                        //var currentAuthList = $('#currentAuthList span');
                        for (var l = 0; l < $scope.info.CurrentAuthorityList.length; l++) {
                            var currentAuth = $scope.info.CurrentAuthorityList[l];
                            //$scope.info.CurrentAuthorityList[l].displayClass = 'btn NoAuth';
                            if ((Number(currentAuth.AuthorityId) & roleAuth) == Number(currentAuth.AuthorityId)) {
                                currentAuth.displayClass = currentAuth.displayClass + ' RoleAuth';
                                //if (!currentAuth.css('RoleAuth')) {
                                //    currentAuth.addClass('RoleAuth');
                                //}
                            }
                            if ((Number(currentAuth.AuthorityId) & data.Authority) == Number(currentAuth.AuthorityId)) {
                                currentAuth.displayClass = currentAuth.displayClass + ' UserAuth';
                                //if (!currentAuth.css('UserAuth')) {
                                //    currentAuth.addClass('UserAuth');
                                //}
                            }
                        }
                        //角色列表
                        for (var p = 0; p < $scope.info.RoleList.length; p++) {
                            $scope.info.RoleList[p].displayClass = 'btn AuthBtn NoAuth';
                            if ((Number($scope.info.RoleList[p].Authority) & roleAuth) == Number($scope.info.RoleList[p].Authority)) {
                                $scope.info.RoleList[p].displayClass = $scope.info.RoleList[p].displayClass + ' UserRole';
                                //if (!currentRole.css('UserRole')) {
                                //    currentRole.addClass('UserRole');
                                //}
                            }
                        }
                        //附加权限列表
                        for (var n = 0; n < $scope.info.AuthorityList.length; n++) {
                            $scope.info.AuthorityList[n].displayClass = 'btn AuthBtn NoAuth';
                            if ((Number($scope.info.AuthorityList[n].AuthorityId) & data.Authority) == Number($scope.info.AuthorityList[n].AuthorityId)) {
                                $scope.info.AuthorityList[n].displayClass = $scope.info.AuthorityList[n].displayClass + ' UserAuth';
                                //if (!curAuth.css('UserAuth')) {
                                //    curAuth.addClass('UserAuth');
                                //}
                            }
                        }

                        $scope.$apply();
                    } else {
                        alert('获取权限列表出错，请刷新页面再重试');
                    }
                },
                error: function(error) {
                    alert(error);
                }
            });
        };

        $scope.refreshRoleAuthority = function (roleId) {
            $.ajax({
                url: '../api/AuthorityAssign?userId=&roleId=' + roleId,
                type: 'GET',
                success: function (data) {
                    if (data != '') {
                        $scope.roleInfo = data;
                        $scope.roleInfo.title = data.Name + "的权限";
                        $scope.$apply();
                        //修改各权限列表的样式(添加颜色)
                        //权限列表
                        for (var n = 0; n < $scope.roleInfo.AuthorityList.length; n++) {
                            $scope.roleInfo.AuthorityList[n].displayClass = 'btn AuthBtn NoAuth';
                            if ((Number($scope.roleInfo.AuthorityList[n].AuthorityId) & data.Authority) == Number($scope.roleInfo.AuthorityList[n].AuthorityId)) {
                                $scope.roleInfo.AuthorityList[n].displayClass = $scope.roleInfo.AuthorityList[n].displayClass + ' RoleAuth';
                                //if (!curAuth.css('UserAuth')) {
                                //    curAuth.addClass('UserAuth');
                                //}
                            }
                        }

                        $scope.$apply();
                    } else {
                        alert('获取权限列表出错，请刷新页面再重试');
                    }
                },
                error: function (error) {
                    alert(error);
                }
            });
        };
    }]);
    //.directive('select2', function (select2Query) {
    //    return {
    //        restrict: 'A',
    //        scope: {
    //            config: '=',
    //            ngModel: '=',
    //            select2Model: '='
    //        },
    //        link: function (scope, element, attrs) {
    //            // 初始化
    //            var tagName = element[0].tagName,
    //                config = {
    //                    allowClear: true,
    //                    multiple: !!attrs.multiple,
    //                    placeholder: attrs.placeholder || ' '   // 修复不出现删除按钮的情况
    //                };

    //            // 生成select
    //            if (tagName === 'SELECT') {
    //                // 初始化
    //                var $element = $(element);
    //                delete config.multiple;

    //                $element
    //                    .prepend('<option value=""></option>')
    //                    .val('')
    //                    .select2(config);

    //                // model - view
    //                scope.$watch('ngModel', function (newVal) {
    //                    setTimeout(function () {
    //                        $element.find('[value^="?"]').remove();    // 清除错误的数据
    //                        $element.select2('val', newVal);
    //                    }, 0);
    //                }, true);
    //                return false;
    //            }

    //            // 处理input
    //            if (tagName === 'INPUT') {
    //                // 初始化
    //                var $element = $(element);

    //                // 获取内置配置
    //                if (attrs.query) {
    //                    scope.config = select2Query[attrs.query]();
    //                }

    //                // 动态生成select2
    //                scope.$watch('config', function () {
    //                    angular.extend(config, scope.config);
    //                    $element.select2('destroy').select2(config);
    //                }, true);

    //                // view - model
    //                $element.on('change', function () {
    //                    scope.$apply(function () {
    //                        scope.select2Model = $element.select2('data');
    //                    });
    //                });

    //                // model - view
    //                scope.$watch('select2Model', function (newVal) {
    //                    $element.select2('data', newVal);
    //                }, true);

    //                // model - view
    //                scope.$watch('ngModel', function (newVal) {
    //                    // 跳过ajax方式以及多选情况
    //                    if (config.ajax || config.multiple) {
    //                        return false; }

    //                    $element.select2('val', newVal);
    //                }, true);
    //            }
    //        }
    //    }
    //});;


angular.bootstrap(angular.element("#AuthorityAssign"), ["AuthorityAssign"]);




function rowNumberFormatter(value, row, index) {
    return '<span>' + (Number(index) + 1) + '</span>';
}




function controlFormatter(value, row, index) {
    var controlFormat = '<button class="btn btn-default  controlBtn detail" data-target="#AuthModal" data-toggle="modal">权限</button>';
    return controlFormat;
}

function controlRoleFormatter(value, row, index) {
    var controlFormat = '<button class="btn btn-default  controlBtn detail" data-target="#RoleModal" data-toggle="modal">权限</button>';
    return controlFormat;
}

var operateEvents = {
    'click .detail': function (e, value, row, index) {
        var ctrlScope = angular.element('[ng-controller=AuthorityAssignController]').scope();
        //ctrlScope.info = row;
        //ctrlScope.info.title = ctrlScope.info.Name + "的权限";
        ctrlScope.refreshUserAuthority(row.Id);
        //ctrlScope.$apply();
    }
};

var operateRoleEvents = {
    'click .detail': function (e, value, row, index) {
        var ctrlScope = angular.element('[ng-controller=AuthorityAssignController]').scope();
        //ctrlScope.roleInfo = row;
        //ctrlScope.roleInfo.title = ctrlScope.roleInfo.Name + "的权限";
        ctrlScope.refreshRoleAuthority(row.Id);
        //ctrlScope.$apply();
    }
};