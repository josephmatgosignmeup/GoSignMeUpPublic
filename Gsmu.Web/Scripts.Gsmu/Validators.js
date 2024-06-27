var Validators = {
    IsNumber: function (n) {
        return !isNaN(parseFloat(n)) && isFinite(n)
    }
}

function checkEmailAvailability(val, field) {
    var validfield = false;
    if (/\s/g.test(val)) {
        val = val.replace(/\s+/g, '');
        Ext.getCmp(field.id).setValue(val);
    }
    var orgnalusername = field.originalValue;
    var newinvalidText = "";
    if (val != "") {
        Ext.Ajax.request({
            async: false,
            method: 'POST',
            url: config.getUrl('public/user/CheckStudentUsernameAvailable'),
            params: {
                Username: val
            },
            success: function (response) {
                if (response.responseText == "available") {
                    field.clearInvalid();
                    validfield = true;
                } else {
                    newinvalidText = 'Username already exist. Please try another.';
                    validfield = false;
                    if (orgnalusername == val) {
                        validfield = true;
                    } else if (response.responseText == "notavailableBB") {
                        validfield = false;
                        newinvalidText = 'Username already exist in either GSMU or Blackboard. Please login with the existing account.';
                    } else if (response.responseText == "notavailableCanvas") {
                        validfield = false;
                        newinvalidText = 'Username already exist in Canvas system. Please login with the existing account.';
                    } else if (response.responseText == "notavailableGSMUorCanvas") {
                        validfield = false;
                        newinvalidText = 'Username already exist in Gosignmeup or Canvas system. Please login with the existing account.';
                    } else if (response.responseText == "invalidhaiku") {
                        validfield = false;
                        newinvalidText = 'Username contains invalid charater(s).';
                    }
                }
                var texterror = newinvalidText;
                UIHandling(texterror, field, validfield);
            },
            failure: function () {
                alert("Error in connection. Pls advice administrator.")
            }
        });
    }
    return validfield;
}

function checkEmailFormat(val) {
    var filter = /^([a-zA-Z0-9_\.\-\w-'])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$/;
    if (filter.test(val)) {
        return true;
    } else {
        return false;
    }
}
function checkPasswordFormat(val) {
    var filter = /^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])[0-9a-zA-Z]{8,}$/;
    if (filter.test(val)) {
        return true;
    } else {
        return false;
    }
}


function checkEmailRestriction(val, field, typeemail) {

    if (val == "") {
        UIHandling("", field, true);
        return;
    }

    // email format
    var validEmailFormat = checkEmailFormat(val);
    if (!validEmailFormat) {
        var validEmailFormatError = 'Invalid e-mail address format "user@example.com"';
    }

    //whitelist
    if (validEmailFormat) {
        var valarr = val.split("@");
        var valdomain = "@" + valarr[1];

        Ext.Ajax.request({
            async: false,
            method: 'POST',
            url: config.getUrl('public/user/CheckEmailRestriction'),
            params: {
                domain: valdomain
            },
            success: function (response) {
                var model = Ext.decode(response.responseText);
                if (model.OnOff == 1) {
                    if (model.valid) {
                        validEmailWhitlist = true;
                        UIHandling("", field, true);
                        if (typeemail == "emailcon2") {
                            checkEmailAvailability(val, field)
                        }

                    } else {
                        validEmailWhitlist = false;
                        //"This email is not listed in the allowed email postfix."
                        var EmailNotification = model.EmailNotification
                        UIHandling(EmailNotification, field, false);
                    }
                } else {
                    validEmailWhitlist = true;
                    UIHandling("", field, true);
                }
            },
            failure: function () {
                alert("Error in connection. Pls advice administrator.")
            }
        });
    } else {
        UIHandling(validEmailFormatError, field, false);
    }
}


function checkPhoneFormat(val, field) {
    if (val != "") {
        var regEx = /^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$|^Ext. [0-9]+$/;
        var texterror = 'Not a valid phone number. Must be in the format (123) 456-7890.';
        UIHandling(texterror, field, regEx.test(val));
    }
}

function checkPasswordFormat(val, field, isSpecialCase, format) {

    if (val == "**********"|| val == "") {
        return true;
    }
    else if (format == "None")
    {
        return true;
    }
    else if (format == "Basic")
    {
        var validfield = true;
        var errmsgall = "";

        var regEx0 = /(?=.*[a-zA-Z])/;
        errmsg0 = " At least one letter </br>"
        if (regEx0.test(val)) {
            errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg0
        } else {
            errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg0
            validfield = false
        }


        var regEx2 = /(?=.*[0-9])/;
        errmsg2 = "At least one number </br>"
        if (regEx2.test(val)) {
            errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg2
        } else {
            errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg2
            validfield = false
        }

        var regEx3 = /[0-9a-zA-Z]{7,}/;
        errmsg3 = "Be at least 7 characters </br>"
        if (val.toString().length >= 7) {
            //}
            //if (regEx3.test(val)) {
            errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg3
        } else {
            errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg3
            validfield = false
        }
        var ErrDisp = Ext.getCmp('errdisp' + field.id);
        //console.log(field)
        if (!validfield) {
            // INVALID
            ErrDisp.setValue('<font style=\'color: #c0272b;font: normal 11px/16px tahoma, arial, verdana, sans-serif;\'>' + errmsgall + '</font>');
            ErrDisp.show();
            ErrorBoxField(field, false)
            return false;
        } else {
            // VALID
            ErrDisp.setValue('');
            ErrDisp.hide();
            ErrorBoxField(field, true)
            return true;
        }
    }
    else {
        var ErrDisp = Ext.getCmp('errdisp' + field.id);
        if (val != "") {
            //var regEx = /^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])[0-9a-zA-Z]{8,}$/;;

            var validfield = true;
            var errmsgall = "";

            var regEx0 = /(?=.*[a-zA-Z])/;
            errmsg0 = " At least one letter </br>"
            if (regEx0.test(val)) {
                errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg0
            } else {
                errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg0
                validfield = false
            }

            var regEx1 = /(?=.*[A-Z])/;
            errmsg1 = "At least one capital letter </br>"
            if (regEx1.test(val)) {
                errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg1
            } else {
                errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg1
                validfield = false
            }

            var regEx2 = /(?=.*[0-9])/;
            errmsg2 = "At least one number </br>"
            if (regEx2.test(val)) {
                errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg2
            } else {
                errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg2
                validfield = false
            }

            var regEx3 = /[0-9a-zA-Z]{8,}/;
            errmsg3 = "Be at least 8 characters </br>"
            if (val.toString().length >= 8) {
                //}
                //if (regEx3.test(val)) {
                errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg3
            } else {
                errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg3
                validfield = false
            }

            if (isSpecialCase) {
                var specialChars = /(?=.*[&#+<>!%=/\\@])/;
                var errmsg4 = 'Must have special characters like (& ! # + < > % = / \ @).'
                if (specialChars.test(val.toString())) {
                    errmsgall += "<img src='/Images/Icons/FamFamFam/accept.png'/> " + errmsg4
                }
                else {
                    errmsgall += "<img src='/Images/Icons/FamFamFam/crossinv.png'/> " + errmsg4
                    validfield = false
                }
            }
            

            //console.log(field)
            if (!validfield) {
                // INVALID
                ErrDisp.setValue('<font style=\'color: #c0272b;font: normal 11px/16px tahoma, arial, verdana, sans-serif;\'>' + errmsgall + '</font>');
                ErrDisp.show();
                ErrorBoxField(field, false)
                return false;
            } else {
                // VALID
                ErrDisp.setValue('');
                ErrDisp.hide();
                ErrorBoxField(field, true)
                return true;
            }

        } else {

            var errmsgall = "This field is required"
            ErrDisp.setValue('<div role="alert" aria-live="polite" id="UserWdgt1Formstudnum-cfrm-errorEl" data-ref="errorEl" class="x-form-error-msg x-form-invalid-under x-form-invalid-under-default" data-anchortarget="UserWdgt1Formstudnum-cfrm-inputEl"><ul class="x-list-plain"><li>This field is required</li></ul></div>');
            ErrDisp.show();
            ErrorBoxField(field, false)
            return false;
        }
    }
}

function UIHandling(message, field, bool) {
    if (bool == false) {
        field.triggerWrap.addCls('x-form-trigger-wrap-invalid');
        field.inputWrap.addCls('x-form-text-wrap-invalid');
        field.inputEl.addCls('x-form-invalid-field').addCls('x-form-invalid-field-default');
        //error message
        var texterror = message;
        field.errorWrapEl.setVisible(true);
        //if (field.errorWrapEl.select('ul').elements.length > 0) {
        //    $('#' + field.errorWrapEl.id + ' > .x-list-plain').text(texterror);
        //}
        //else {
        //    //$('#' + field.errorWrapEl.id).append('<ul class="x-list-plain" style="color:#c0272b;font: 11px/16px tahoma,arial,verdana,sans-serif"><li>' + texterror + '</li></ul>');
        //}
        $('#' + field.errorEl.id).append('<ul class="x-list-plain" style="color:#c0272b;font: 11px/16px tahoma,arial,verdana,sans-serif"><li>' + texterror + '</li></ul>');
    }
    else {
        field.triggerWrap.removeCls('x-form-trigger-wrap-invalid');
        field.inputWrap.removeCls('x-form-text-wrap-invalid');
        field.inputEl.removeCls('x-form-invalid-field').removeCls('x-form-invalid-field-default');
        field.errorWrapEl.setVisible(false);
        $('#' + field.errorWrapEl.id + ' > .x-list-plain').text('');
    }
}

function ErrorBoxField(field, bool) {
    if (bool == false) {
        field.triggerWrap.addCls('x-form-trigger-wrap-invalid');
        field.inputWrap.addCls('x-form-text-wrap-invalid');
        field.inputEl.addCls('x-form-invalid-field').addCls('x-form-invalid-field-default');
    }
    else {
        field.triggerWrap.removeCls('x-form-trigger-wrap-invalid');
        field.inputWrap.removeCls('x-form-text-wrap-invalid');

        field.inputEl.removeCls('x-form-invalid-field').removeCls('x-form-invalid-field-default');
    }
}