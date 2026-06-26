import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    ContractKolServiceProxy,
    ContractKolDto,
    ContractKolDtoPagedResultDto,
    ContractKolStatus,
    ReceiveStatus,
    ChannelType,
    KolServiceProxy,
    ContractServiceProxy,
    ContractKolResultServiceProxy,
    ContractKolResultDto,
    CreateContractKolResultDto,
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
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Tooltip } from 'primeng/tooltip';
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
        IconField,
        InputIcon,
        InputText,
        InputNumber,
        DatePicker,
        Tooltip,
        CommonModule,
    ],
})
export class ContractKolsComponent extends PagedListingComponentBase<ContractKolDto> implements OnInit {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    filterStatus: ContractKolStatus | undefined = undefined;
    filterContractId: string | undefined = undefined;
    advancedFiltersVisible = false;

    kolMap: Record<string, string> = {};
    contractMap: Record<string, string> = {};
    contractOptions: { value: string | undefined; label: string }[] = [];

    editingRowKeys: { [s: string]: boolean } = {};
    clonedRecords: Record<string, ContractKolDto> = {};
    airTimeDates: Record<string, Date | null> = {};

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

    constructor(
        injector: Injector,
        private _contractKolService: ContractKolServiceProxy,
        private _kolService: KolServiceProxy,
        private _contractService: ContractServiceProxy,
        private _contractKolResultService: ContractKolResultServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    ngOnInit(): void {
        this._kolService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            (r.items ?? []).forEach((k) => { this.kolMap[k.id] = k.name ?? k.id; });
        });
        this._contractService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            this.contractOptions = [
                { value: undefined, label: 'Tất cả' },
                ...(r.items ?? []).map((c) => ({ value: c.id, label: c.name ?? c.id })),
            ];
            (r.items ?? []).forEach((c) => { this.contractMap[c.id] = c.name ?? c.id; });
        });
    }

    createContractKol(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateContractKolDialogComponent, { class: 'modal-xl' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    viewDetail(record: ContractKolDto): void {
        this._modalService.show(ViewContractKolDetailDialogComponent, {
            class: 'modal-lg',
            initialState: {
                record: record,
                kolName: this.getKolName(record.kolId),
                contractName: this.getContractName(record.contractId),
            },
        });
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

    getKolName(kolId: string | undefined): string {
        return kolId ? (this.kolMap[kolId] ?? kolId) : '—';
    }

    getContractName(contractId: string | undefined): string {
        return contractId ? (this.contractMap[contractId] ?? contractId) : '—';
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

    delete(ck: ContractKolDto): void {
        abp.message.confirm(
            `Xóa KOL "${this.getKolName(ck.kolId)}" khỏi hợp đồng "${this.getContractName(ck.contractId)}"?`,
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
