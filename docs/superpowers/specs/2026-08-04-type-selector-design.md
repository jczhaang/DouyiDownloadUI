# 类型字段选择式改造 设计规格

> 日期：2026-08-04
> 状态：已确认，待写实施计划
> 项目记忆：`AGENTS.md`

## 1. 概述

将命名步的"类型"字段从手动输入的 TextBox 改为不可编辑的 ComboBox（下拉选择）。选项列表存储在 `AppSettings.TypeOptions` 中，在设置页可增删管理。默认提供五个选项：中三、中四、平四、三步、其他。

## 2. 需求

| 编号 | 需求 |
| --- | --- |
| FR-T1 | 类型字段为下拉选择（ComboBox, IsEditable=False），不可手动输入 |
| FR-T2 | 选项列表存储在 settings.json 的 `TypeOptions` 字段，默认 `["中三", "中四", "平四", "三步", "其他"]` |
| FR-T3 | 设置页新增"类型选项"分区：显示所有选项（每行一个 + 删除按钮），底部有输入框 + 添加按钮 |
| FR-T4 | 进入命名步时，默认选中上次用过的类型（`LastType`）；若 `LastType` 不在列表中则选第一项 |
| FR-T5 | 下载完成后仍记住 `LastType`（已有逻辑不变） |
| FR-T6 | 老配置文件（无 `TypeOptions` 字段）加载时自动初始化为 5 个默认值 |

## 3. 数据层改动

### 3.1 AppSettings（`Core/Models.cs`）

- **新增**：`TypeOptions`（`List<string>`），默认 `["中三", "中四", "平四", "三步", "其他"]`
- **移除**：`RecentTypes`（不再需要"记忆用过的类型"机制）
- **保留**：`LastType`（记住上次选的类型）

### 3.2 SettingsService（`Services/SettingsService.cs`）

- `Load()` 中：若 `TypeOptions` 为 null 或空，初始化为 5 个默认值
- `Load()` 中：移除 `RecentTypes` 的兼容性处理
- `CreateDefault()` 中：设置 `TypeOptions` 为 5 个默认值

## 4. ViewModel 层改动

### 4.1 MainViewModel

- `RecentTypes` → `TypeOptions`（`ObservableCollection<string>`），绑定到 ComboBox
- `Type` 属性保持 string 类型，绑定 ComboBox 的 `SelectedItem`
- `NextAsync` 中：默认选中 `LastType`（若存在于 TypeOptions 中），否则选第一项
- 移除 `RememberType` 方法（选项是预设的，不需要记忆用过的类型）
- 移除 `RefreshRecentTypes` 方法，替换为 `RefreshTypeOptions`
- 下载完成后 `_config.LastType = Type` 逻辑不变

### 4.2 SettingsViewModel

- 新增 `TypeOptions`（`ObservableCollection<string>`）
- 新增 `NewType`（string，绑定设置页输入框）
- 新增 `AddTypeCommand`：将 `NewType` 追加到 `TypeOptions` 并保存到 settings.json
- 新增 `RemoveTypeCommand(string type)`：从 `TypeOptions` 移除并保存
- `Refresh()` 中加载 `TypeOptions`

## 5. UI 层改动

### 5.1 MainWindow.xaml——命名步类型列

当前：
```xml
<TextBox Text="{Binding Type, UpdateSourceTrigger=PropertyChanged}" Padding="6"/>
```

改为：
```xml
<ComboBox IsEditable="False"
          ItemsSource="{Binding TypeOptions}"
          SelectedItem="{Binding Type}"
          Padding="6"/>
```

### 5.2 SettingsWindow.xaml——新增"类型选项"分区

在"字体大小"和"疑难解答"之间新增：

```
┌─────────────────────────────────────┐
│ 🏷 类型选项                          │
│                                     │
│  中三      [删除]                    │
│  中四      [删除]                    │
│  平四      [删除]                    │
│  三步      [删除]                    │
│  其他      [删除]                    │
│                                     │
│  [输入新类型...]  [添加]             │
└─────────────────────────────────────┘
```

- 选项列表用 `ItemsControl`，每行一个 `TextBlock` + 删除按钮
- 底部 `TextBox`（绑定 `NewType`）+ "添加"按钮（绑定 `AddTypeCommand`）

## 6. 向后兼容

老用户的 `settings.json` 没有 `TypeOptions` 字段：
- `Load()` 检测到 `TypeOptions` 为 null 或空时，自动填充 5 个默认值
- `RecentTypes` 字段被忽略（不再读取，JSON 中残留不影响）

## 7. 测试策略

### 单元测试（xUnit）

- **ModelsTests**：`AppSettings` 默认 `TypeOptions` 包含 5 个默认值
- **SettingsServiceTests**：
  - 加载无 `TypeOptions` 的老配置文件 → 自动初始化默认值
  - 保存/加载 `TypeOptions` 往返一致
- **MainViewModelTests**：
  - 进入命名步时 `Type` 默认选中 `LastType`
  - `LastType` 不在 `TypeOptions` 中时选中第一项
  - `TypeOptions` 列表正确填充
  - 下载完成后 `LastType` 记住选择的类型
- **SettingsViewModelTests**：
  - `AddType` 追加新类型并持久化
  - `RemoveType` 删除指定类型并持久化

### 手动测试

- 设置页添加/删除类型 → 主窗口下拉框同步更新
- 选择类型 → 下载 → 文件名包含选择的类型
- 老配置文件升级 → 类型选项自动出现

## 8. 范围外

- 不支持类型排序/拖拽排序
- 不支持类型重命名（删除后重新添加即可）
- 不限制类型数量上限（用户自行管理）
