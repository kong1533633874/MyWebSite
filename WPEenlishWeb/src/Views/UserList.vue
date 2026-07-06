<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">用户管理</h2>
      <div class="page-actions">
        <el-button type="primary" @click="add">+ 添加用户</el-button>
      </div>
    </div>

    <div class="card">
      <el-table :data="state.tableData" style="width: 100%" stripe>
        <el-table-column prop="userName" label="用户名" />
        <el-table-column label="创建时间" width="180">
          <template #default="scope">{{ formatTime(scope.row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="140">
          <template #default="scope">
            <el-button link type="primary" size="small" @click="edit(scope.row)">更改密码</el-button>
            <el-button link type="danger" size="small" @click="dele(scope.row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import { onMounted, reactive } from 'vue'
import {useRouter} from 'vue-router'
import ConfirmBox from '@/utils/ConfirmBox';
import { formatTime } from '@/utils/time';

const state = reactive({
	tableData:[]
})

const router = useRouter()

onMounted(async () => {
	fetchCategories()
})

const fetchCategories = async () => {
	try {
		const response = await request.get("/identity/FindAllUsers")
		if(response.data.data){
			state.tableData = response.data.data
		}else{
			state.tableData = []
		}
		
	} catch (error) {
		ElMessage.error("获取用户列表失败,请稍后重试")
	}
}

const add = () => {
	router.push({path:"/userAdd"})
}

const edit = async(row)=>{
	router.push({path:"/userUpdate",query:row})
}

const dele = async (id) => {
  const confirm = await ConfirmBox("确定删除吗")
  if(!confirm) return
	try{
		const response = await request.delete(`/identity/Delete?id=${id}`)
		if(response.status === 200){
			ElMessage.success("删除成功")
			fetchCategories()
		}
	}catch(error){
		ElMessage.error("删除失败, 请检查网络连接")
	}
}

</script>

<style scoped>
.page-container {
  max-width: 960px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
  letter-spacing: -0.3px;
}

.card {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  padding: 24px;
}
</style>
