import { Component, Injector, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    ContractKolServiceProxy,
    ContractKolDto,
    CreateContractKolDto,
    ContractKolStatus,
    ContractServiceProxy,
    ContractDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import moment from 'moment';

@Component({
    templateUrl: './add-kol-to-contracts-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        CommonModule,
        AbpModalHeaderComponent,
        LocalizePipe,
        Button,
        Select,
        Tag,
    ],
})
export class AddKolToContractsDialogComponent extends AppComponentBase implements OnInit, OnDestroy {
    private destroy$ = new Subject<void>();
    kolId: string;
    kolName: string = '';

    loading = false;
    saving = false;

    existingList: ContractKolDto[] = [];
    contracts: ContractDto[] = [];
    contractMap: Record<string, string> = {};
    contractOptions: { value: string; label: string }[] = [];

    selectedContractId: string | undefined;
    selectedStatus: ContractKolStatus = ContractKolStatus.Register;

    statusOptions = [
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

    constructor(
        injector: Injector,
        public bsModalRef: BsModalRef,
        private _contractKolService: ContractKolServiceProxy,
        private _contractService: ContractServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.loading = true;
        this.existingList = [];
        this.contracts = [];
        this.contractMap = {};
        this.contractOptions = [];
        this.selectedContractId = undefined;
        this.selectedStatus = ContractKolStatus.Register;

        this._contractService.getAll(undefined, undefined, undefined, undefined, 0, 1000)
            .pipe(takeUntil(this.destroy$))
            .subscribe((r) => {
                this.contracts = r.items ?? [];
                this.contracts.forEach((c) => { this.contractMap[c.id] = c.name ?? c.id; });
                this.loadExisting();
            });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private loadExisting(): void {
        this._contractKolService.getAll(this.kolId, undefined, undefined, undefined, 0, 1000)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (r) => {
                    this.existingList = r.items ?? [];
                    this.rebuildOptions();
                    this.loading = false;
                    this.cd.detectChanges();
                },
                error: () => { this.loading = false; this.cd.detectChanges(); },
            });
    }

    private rebuildOptions(): void {
        const usedIds = new Set(this.existingList.map((e) => e.contractId));
        this.contractOptions = this.contracts
            .filter((c) => !usedIds.has(c.id))
            .map((c) => ({ value: c.id, label: c.name ?? c.id }));
    }

    add(): void {
        if (!this.selectedContractId) return;
        this.saving = true;
        const dto = new CreateContractKolDto();
        dto.kolId = this.kolId;
        dto.contractId = this.selectedContractId;
        dto.status = this.selectedStatus;
        this._contractKolService.create(dto).subscribe({
            next: (created) => {
                this.existingList.push(created);
                this.rebuildOptions();
                this.selectedContractId = undefined;
                this.selectedStatus = ContractKolStatus.Register;
                this.saving = false;
                this.notify.info(this.l('SavedSuccessfully'));
                this.cd.detectChanges();
            },
            error: () => { this.saving = false; this.cd.detectChanges(); },
        });
    }

    remove(ck: ContractKolDto): void {
        abp.message.confirm(
            `Xóa khỏi hợp đồng "${this.getContractName(ck.contractId)}"?`,
            undefined,
            (ok: boolean) => {
                if (!ok) return;
                this._contractKolService.delete(ck.id).subscribe(() => {
                    this.existingList = this.existingList.filter((e) => e.id !== ck.id);
                    this.rebuildOptions();
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.cd.detectChanges();
                });
            }
        );
    }

    getContractName(contractId: string | undefined): string {
        return contractId ? (this.contractMap[contractId] ?? contractId) : '—';
    }

    getStatusLabel(s: ContractKolStatus | undefined): string {
        return s != null ? (this.statusLabels[s] ?? '') : '—';
    }

    getStatusSeverity(s: ContractKolStatus | undefined): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        return s != null ? (this.statusSeverities[s] ?? 'secondary') : 'secondary';
    }
}
