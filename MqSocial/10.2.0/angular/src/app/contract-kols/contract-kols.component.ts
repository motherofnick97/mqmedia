import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { forkJoin } from 'rxjs';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import * as XLSX from 'xlsx';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    ContractKolServiceProxy,
    ContractKolDto,
    ContractKolDtoPagedResultDto,
    ContractKolStatus,
    ReceiveStatus,
    ChannelType,
    ContractServiceProxy,
    ContractKolResultServiceProxy,
    ContractKolResultDto,
    CreateContractKolResultDto,
    SendContractKolsEmailDto,
} from '@shared/service-proxies/service-proxies';
import { CreateContractKolDialogComponent } from './create-contract-kol/create-contract-kol-dialog.component';
import { ViewContractKolDetailDialogComponent } from './view-contract-kol-detail/view-contract-kol-detail-dialog.component';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Toolbar } from 'primeng/toolbar';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Tooltip } from 'primeng/tooltip';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import moment from 'moment';

@Component({
    templateUrl: './contract-kols.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        FormsModule,
        TableModule,
        PrimeTemplate,
        PaginatorModule,
        LocalizePipe,
        Button,
        Select,
        Tag,
        Toolbar,
        InputText,
        InputNumber,
        DatePicker,
        Tooltip,
        Dialog,
        Textarea,
        CommonModule,
    ],
})
export class ContractKolsComponent extends PagedListingComponentBase<ContractKolDto> implements OnInit {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    filterStatus: ContractKolStatus | undefined = undefined;
    filterContractId: string | undefined = undefined;

    contractOptions: { value: string | undefined; label: string }[] = [];

    editingRowKeys: { [s: string]: boolean } = {};
    clonedRecords: Record<string, ContractKolDto> = {};
    airTimeDates: Record<string, Date | null> = {};

    exporting = false;

    // Row expansion — ContractKolResult
    expandedRows: Record<string, boolean> = {};
    resultsMap: Record<string, ContractKolResultDto[]> = {};
    loadingResultsMap: Record<string, boolean> = {};
    savingResultMap: Record<string, boolean> = {};
    newResultMap: Record<string, CreateContractKolResultDto> = {};
    newResultDateMap: Record<string, Date | null> = {};

    statusOptions = [
        { value: undefined, label: 'Tất cả' },
        { value: ContractKolStatus.Register, label: 'Đăng ký' },
        { value: ContractKolStatus.Approve, label: 'Duyệt đăng ký' },
        { value: ContractKolStatus.Processing, label: 'Đang tiến hành' },
        { value: ContractKolStatus.MktOk, label: 'Mkt duyệt' },
        { value: ContractKolStatus.DpmOk, label: 'Quản lý duyệt' },
        { value: ContractKolStatus.OnAir, label: 'Đã air' },
        { value: ContractKolStatus.Following, label: 'Theo dõi' },
        { value: ContractKolStatus.Paid, label: 'Đã thanh toán' },
        { value: ContractKolStatus.Done, label: 'Hoàn thành' },
        { value: ContractKolStatus.Cancel, label: 'Hủy' },
        { value: ContractKolStatus.Reject, label: 'Từ chối' },
    ];

    editStatusOptions = this.statusOptions.filter(o => o.value !== undefined);

    readonly statusLabels: Record<number, string> = {
        [ContractKolStatus.Register]: 'Đăng ký',
        [ContractKolStatus.Approve]: 'Duyệt đăng ký',
        [ContractKolStatus.Processing]: 'Đang tiến hành',
        [ContractKolStatus.MktOk]: 'Mkt duyệt',
        [ContractKolStatus.DpmOk]: 'Quản lý duyệt',
        [ContractKolStatus.OnAir]: 'Đã air',
        [ContractKolStatus.Following]: 'Theo dõi',
        [ContractKolStatus.Paid]: 'Đã thanh toán',
        [ContractKolStatus.Done]: 'Hoàn thành',
        [ContractKolStatus.Cancel]: 'Hủy',
        [ContractKolStatus.Reject]: 'Từ chối',
    };

    readonly statusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
        [ContractKolStatus.Register]: 'secondary',
        [ContractKolStatus.Approve]: 'info',
        [ContractKolStatus.Processing]: 'info',
        [ContractKolStatus.MktOk]: 'warn',
        [ContractKolStatus.DpmOk]: 'warn',
        [ContractKolStatus.OnAir]: 'success',
        [ContractKolStatus.Following]: 'secondary',
        [ContractKolStatus.Paid]: 'success',
        [ContractKolStatus.Done]: 'success',
        [ContractKolStatus.Cancel]: 'danger',
        [ContractKolStatus.Reject]: 'danger',
    };

    channelOptions = [
        { value: ChannelType.Tiktok, label: 'TikTok' },
        { value: ChannelType.Facebook, label: 'Facebook' },
        { value: ChannelType.Khac, label: 'Khác' },
    ];

    readonly channelLabels: Record<number, string> = {
        [ChannelType.Tiktok]: 'TikTok',
        [ChannelType.Facebook]: 'Facebook',
        [ChannelType.Khac]: 'Khác',
    };

    receiveStatusOptions = [
        { value: ReceiveStatus.NotShip, label: 'Chưa gửi' },
        { value: ReceiveStatus.Shipping, label: 'Đang gửi' },
        { value: ReceiveStatus.Received, label: 'Đã nhận' },
        { value: ReceiveStatus.NotReceived, label: 'Không nhận' },
    ];

    readonly receiveStatusLabels: Record<number, string> = {
        [ReceiveStatus.NotShip]: 'Chưa gửi',
        [ReceiveStatus.Shipping]: 'Đang gửi',
        [ReceiveStatus.Received]: 'Đã nhận',
        [ReceiveStatus.NotReceived]: 'Không nhận',
    };

    readonly receiveStatusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'danger'> = {
        [ReceiveStatus.NotShip]: 'secondary',
        [ReceiveStatus.Shipping]: 'info',
        [ReceiveStatus.Received]: 'success',
        [ReceiveStatus.NotReceived]: 'danger',
    };

    // Email dialog
    emailDialogVisible = false;
    emailToText = '';
    emailSubject = 'Danh sách KOL Hợp đồng';
    emailBody = 'Vui lòng xem file đính kèm.';
    sending = false;

    constructor(
        injector: Injector,
        private _contractKolService: ContractKolServiceProxy,
        private _contractService: ContractServiceProxy,
        private _contractKolResultService: ContractKolResultServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    ngOnInit(): void {
        // Chỉ dùng để build danh sách lọc theo hợp đồng — tên KOL/hợp đồng của từng dòng
        // đã có sẵn trong kết quả trả về của contractKolService.getAll (kolName/contractName).
        this._contractService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            this.contractOptions = [
                { value: undefined, label: 'Tất cả' },
                ...(r.items ?? []).map((c) => ({ value: c.id, label: c.name ?? c.id })),
            ];
        });
    }

    createContractKol(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateContractKolDialogComponent, { class: 'modal-xl' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    viewDetail(record: ContractKolDto): void {
        const modalRef = this._modalService.show(ViewContractKolDetailDialogComponent, {
            class: 'modal-xl',
            initialState: {
                kolName: this.getKolName(record),
                contractName: this.getContractName(record),
            },
        });
        modalRef.content.setRecord(record);
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    toggleExpand(record: ContractKolDto): void {
        const id = record.id;
        if (this.expandedRows[id]) {
            this.expandedRows[id] = false;
            delete this.newResultMap[id];
            delete this.newResultDateMap[id];
        } else {
            this.expandedRows[id] = true;
            if (!this.resultsMap[id]) {
                this.loadResults(id);
            }
            this.initNewResultForm(id);
        }
        this.cd.detectChanges();
    }

    private loadResults(contractKolId: string): void {
        this.loadingResultsMap[contractKolId] = true;
        this._contractKolResultService.getAll(contractKolId, undefined, undefined, 0, 500).subscribe({
            next: (r) => {
                this.resultsMap[contractKolId] = r.items ?? [];
                this.loadingResultsMap[contractKolId] = false;
                this.cd.detectChanges();
            },
            error: () => { this.loadingResultsMap[contractKolId] = false; this.cd.detectChanges(); },
        });
    }

    private initNewResultForm(contractKolId: string): void {
        const dto = new CreateContractKolResultDto();
        dto.contractKolId = contractKolId;
        dto.channelType = ChannelType.Tiktok;
        this.newResultMap[contractKolId] = dto;
        this.newResultDateMap[contractKolId] = null;
    }

    saveResult(contractKolId: string): void {
        const dto = this.newResultMap[contractKolId];
        if (!dto) return;
        dto.postTime = this.newResultDateMap[contractKolId]
            ? moment(this.newResultDateMap[contractKolId])
            : undefined;
        this.savingResultMap[contractKolId] = true;
        this._contractKolResultService.create(dto).subscribe({
            next: (created) => {
                if (!this.resultsMap[contractKolId]) this.resultsMap[contractKolId] = [];
                this.resultsMap[contractKolId].push(created);
                this.initNewResultForm(contractKolId);
                this.savingResultMap[contractKolId] = false;
                this.notify.info(this.l('SavedSuccessfully'));
                this.cd.detectChanges();
            },
            error: () => { this.savingResultMap[contractKolId] = false; this.cd.detectChanges(); },
        });
    }

    deleteResult(result: ContractKolResultDto): void {
        abp.message.confirm('Xóa kết quả này?', undefined, (ok: boolean) => {
            if (!ok) return;
            this._contractKolResultService.delete(result.id).subscribe(() => {
                const list = this.resultsMap[result.contractKolId];
                if (list) {
                    const idx = list.findIndex(r => r.id === result.id);
                    if (idx !== -1) list.splice(idx, 1);
                }
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.cd.detectChanges();
            });
        });
    }

    getChannelLabel(c: ChannelType | undefined): string {
        return c != null ? (this.channelLabels[c] ?? '') : '—';
    }

    initRow(record: ContractKolDto): void {
        this.clonedRecords[record.id] = record.clone();
        this.airTimeDates[record.id] = record.airTime ? record.airTime.toDate() : null;
        this.editingRowKeys = { ...this.editingRowKeys, [record.id]: true };
    }

    saveRow(record: ContractKolDto, index: number): void {
        record.airTime = this.airTimeDates[record.id] ? moment(this.airTimeDates[record.id]) : undefined;
        this._contractKolService.update(record).subscribe({
            next: () => {
                delete this.clonedRecords[record.id];
                delete this.airTimeDates[record.id];
                const keys = { ...this.editingRowKeys };
                delete keys[record.id];
                this.editingRowKeys = keys;
                this.notify.info(this.l('SavedSuccessfully'));
                this.cd.detectChanges();
            },
            error: () => {
                this.restoreRecord(record.id, index);
                this.cd.detectChanges();
            },
        });
    }

    cancelRow(record: ContractKolDto, index: number): void {
        this.restoreRecord(record.id, index);
        const keys = { ...this.editingRowKeys };
        delete keys[record.id];
        this.editingRowKeys = keys;
    }

    private restoreRecord(id: string, index: number): void {
        const clone = this.clonedRecords[id];
        if (clone && this.primengTableHelper.records) {
            this.primengTableHelper.records[index] = clone;
        }
        delete this.clonedRecords[id];
        delete this.airTimeDates[id];
        this.cd.detectChanges();
    }

    clearFilters(): void {
        this.filterStatus = undefined;
        this.filterContractId = undefined;
        this.list();
    }

    getKolName(record: ContractKolDto): string {
        return record.kolName ?? '—';
    }

    getContractName(record: ContractKolDto): string {
        return record.contractName ?? '—';
    }

    getStatusLabel(status: ContractKolStatus): string {
        return this.statusLabels[status] ?? '';
    }

    getStatusSeverity(status: ContractKolStatus): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        return this.statusSeverities[status] ?? 'secondary';
    }

    getReceiveStatusLabel(s: ReceiveStatus | undefined): string {
        return s != null ? (this.receiveStatusLabels[s] ?? '') : '—';
    }

    getReceiveStatusSeverity(s: ReceiveStatus | undefined): 'secondary' | 'info' | 'success' | 'danger' {
        return s != null ? (this.receiveStatusSeverities[s] ?? 'secondary') : 'secondary';
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._contractKolService
            .getAll(
                undefined,
                this.filterContractId,
                this.filterStatus,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: ContractKolDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    openSendEmailDialog(): void {
        if (!this.filterContractId) {
            abp.message.warn('Vui lòng lọc theo hợp đồng trước khi gửi mail.');
            return;
        }
        this.emailToText = '';
        this.emailSubject = 'Danh sách KOL Hợp đồng';
        this.emailBody = '';
        this.emailDialogVisible = true;
    }

    sendEmail(): void {
        const recipients = this.emailToText.split(',').map(e => e.trim()).filter(e => !!e);
        if (!recipients.length) {
            abp.message.warn('Vui lòng nhập ít nhất một địa chỉ email.');
            return;
        }
        if (!this.filterContractId) {
            abp.message.warn('Vui lòng lọc theo hợp đồng trước khi gửi mail.');
            return;
        }

        const dto = new SendContractKolsEmailDto();
        dto.contractId = this.filterContractId;
        dto.status = this.filterStatus;
        dto.to = recipients;
        dto.subject = this.emailSubject;
        dto.body = this.emailBody;

        this.sending = true;
        this._contractKolService.sendListEmail(dto)
            .pipe(finalize(() => { this.sending = false; this.cd.detectChanges(); }))
            .subscribe({
                next: () => {
                    this.emailDialogVisible = false;
                    this.notify.success('Email đã được gửi thành công!');
                },
                error: () => { this.cd.detectChanges(); },
            });
    }

    exportExcel(): void {
        this.exporting = true;
        forkJoin({
            kols: this._contractKolService.getAll(undefined, this.filterContractId, this.filterStatus, undefined, 0, 10000),
            results: this._contractKolResultService.getAll(undefined, undefined, undefined, 0, 50000),
        })
        .pipe(finalize(() => { this.exporting = false; this.cd.detectChanges(); }))
        .subscribe(({ kols, results }) => {
            const allKols = kols.items ?? [];
            const resultsByKolId: Record<string, ContractKolResultDto[]> = {};
            (results.items ?? []).forEach(r => {
                if (!resultsByKolId[r.contractKolId]) resultsByKolId[r.contractKolId] = [];
                resultsByKolId[r.contractKolId].push(r);
            });

            const rows: any[] = [];
            for (const kol of allKols) {
                const kolResults = resultsByKolId[kol.id] ?? [];
                if (kolResults.length === 0) {
                    rows.push(this.buildExportRow(kol, null));
                } else {
                    kolResults.forEach(r => rows.push(this.buildExportRow(kol, r)));
                }
            }

            const ws = XLSX.utils.json_to_sheet(rows);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'ContractKols');
            XLSX.writeFile(wb, `ContractKols_${new Date().toISOString().slice(0, 10)}.xlsx`);
        });
    }

    private buildExportRow(kol: ContractKolDto, result: ContractKolResultDto | null): any {
        return {
            'KOL': this.getKolName(kol),
            'Hợp đồng': this.getContractName(kol),
            'Trạng thái': kol.status != null ? this.getStatusLabel(kol.status) : '',
            'Cash': kol.cash ?? 0,
            'Payment': kol.payment ?? 0,
            'Air Time': kol.airTime ? kol.airTime.format('DD/MM/YYYY') : '',
            'Brief Link': kol.briefLink ?? '',
            'Brief': kol.brief ?? '',
            'Feedback': kol.feedback ?? '',
            'Caption': kol.caption ?? '',
            'Hashtag': kol.hashTag ?? '',
            'Portrait': kol.portrait ?? '',
            'Review Corner': kol.reviewCorner ?? '',
            'Tên mẫu': kol.sampleName ?? '',
            'Size mẫu': kol.sampleSize ?? '',
            'SL mẫu': kol.sampleQuantity ?? 0,
            'Nhận mẫu': kol.sampleReceiveStatus != null ? this.getReceiveStatusLabel(kol.sampleReceiveStatus) : '',
            'Kết quả review': kol.reviewResult ?? '',
            'Ngày đăng (Result)': result?.postTime ? result.postTime.format('DD/MM/YYYY') : '',
            'Kênh (Result)': result ? this.getChannelLabel(result.channelType) : '',
            'Link bài': result?.postLink ?? '',
            'View': result?.view ?? '',
            'Comment': result?.comment ?? '',
            'Like': result?.like ?? '',
            'Save': result?.save ?? '',
            'Share': result?.share ?? '',
        };
    }

    delete(ck: ContractKolDto): void {
        abp.message.confirm(
            `Xóa KOL "${this.getKolName(ck)}" khỏi hợp đồng "${this.getContractName(ck)}"?`,
            undefined,
            (result: boolean) => {
                if (result) {
                    this._contractKolService.delete(ck.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
