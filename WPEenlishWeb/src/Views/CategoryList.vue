<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">分类管理</h2>
      <div class="page-actions">
        <el-button v-if="isSorting" @click="cancelSort">取消排序</el-button>
        <el-button @click="toggleSort" :type="isSorting ? 'warning' : 'default'">{{ isSorting ? '保存排序' : '排序' }}</el-button>
        <el-button v-if="!isSorting" type="primary" @click="add">+ 添加分类</el-button>
      </div>
    </div>

    <div class="card">
      <el-table :data="state.tableData" style="width: 100%" stripe>
        <el-table-column label="序号" width="80">
          <template #default="scope">{{ scope.$index + 1 }}</template>
        </el-table-column>
        <el-table-column prop="title" label="标题" />
        <el-table-column prop="createTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="200">
          <template #default="scope">
            <template v-if="isSorting">
              <el-button link type="primary" size="small" :disabled="scope.$index === 0" @click="moveUp(scope.$index)">上移</el-button>
              <el-button link type="primary" size="small" :disabled="scope.$index === state.tableData.length - 1" @click="moveDown(scope.$index)">下移</el-button>
            </template>
            <template v-else>
              <el-button link type="primary" size="small" @click="edit(scope.row)">编辑</el-button>
              <el-button link type="danger" size="small" @click="dele(scope.row.id)">删除</el-button>
              <el-button link type="primary" size="small" @click="manage(scope.row.id)">管理专辑</el-button>
            </template>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { onMounted, reactive, ref } from 'vue'
import {useRouter} from 'vue-router'
import { ElMessage } from 'element-plus'
import ConfirmBox from '@/utils/ConfirmBox';

const state = reactive({
	tableData:[]
})

const isSorting = ref(false)

const router = useRouter()

onMounted(async () => {
	fetchCategories()
})

const fetchCategories = async () => {
	try {
		const response = await request.get("/admin/category/GetAllCategories")
		if (response.data.data) {
			state.tableData = response.data.data.filter(s => !s.isDeleted)
		} else {
			state.tableData = []
		}
	} catch (error) {
		ElMessage.error("获取分类列表失败，请稍后重试")
	}
}
const add = () => {
	router.push({path:"/categoryAdd"})
}
const edit = async(row)=>{
	router.push({path:"/categoryUpdate",query:row})
}
const dele = async (id) => {
  const confirm = await ConfirmBox("确定删除吗")
  if(!confirm) return 
	try {
		const response = await request.delete(`/admin/category/Delete?id=${id}`)
		if (response.status === 200) {
			ElMessage.success("删除成功")
			fetchCategories()
		} else {
			ElMessage.warning("删除失败，请稍后重试")
		}
	} catch (error) {
		ElMessage.error("删除失败，请检查网络连接")
	}
}

const cancelSort = () => {
	isSorting.value = false
	fetchCategories()
}

const toggleSort = async () => {
	if (!isSorting.value) {
		isSorting.value = true
		return
	}
	const sortedIds = state.tableData.map(item => item.id)
	try {
		const response = await request.put("/admin/category/Reorder", {
			sortedCategoryIds: sortedIds
		})
		if (response.status === 200) {
			ElMessage.success("排序保存成功")
			fetchCategories()
		}
	} catch (error) {
		ElMessage.error("排序保存失败")
	}
	isSorting.value = false
}

const moveUp = (index) => {
	if (index <= 0) return
	const list = state.tableData
	;[list[index - 1], list[index]] = [list[index], list[index - 1]]
}

const moveDown = (index) => {
	if (index >= state.tableData.length - 1) return
	const list = state.tableData
	;[list[index], list[index + 1]] = [list[index + 1], list[index]]
}

const manage = async(id)=>{
	router.push({path:"/albumList",query:{categoryId:id}})
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
