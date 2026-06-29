import { ElMessageBox } from "element-plus";

async function ConfirmBox(message){
    try{
        await ElMessageBox.confirm(message,"提示",{
            confirmButtonText:"确定",
            cancelButtonText:"取消",
            type:'warning'
        })

        return true
    }catch{
        return false
    }
}

export default ConfirmBox